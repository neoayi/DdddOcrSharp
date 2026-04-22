using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using System.Diagnostics;
using System.Text;

namespace DdddOcrSharp
{
    /// <summary>
    /// DDDDOCR类
    /// </summary>
    public partial class DDDDOCR : IDisposable
    {
        private DdddOcrMode Mode { get; }
        private DdddOcrOptions? Options { get; }
        private SessionOptions? SessionOptions { get; }
        private InferenceSession InferenceSession { get; }

        #region Basic

        /// <summary>
        /// DDDDOCR使用自带模型实例化
        /// </summary>
        /// <param name="mode">实例化模型</param>
        /// <param name="use_gpu">是否使用GPU</param>
        /// <param name="device_id">GPU的ID</param>
        /// <exception cref="NotSupportedException">不支持模式报错</exception>
        /// <exception cref="FileNotFoundException">模型文件路径不存在报错</exception>
        public DDDDOCR(DdddOcrMode mode, bool use_gpu = false, int device_id = 0)
        {
#if DEBUG
            Trace.WriteLine($"欢迎使用ddddocr，本项目专注带动行业内卷");
            Trace.WriteLine($"python版开发作者：https://github.com/sml2h3/ddddocr");
            Trace.WriteLine($"C#/NET版移植作者：https://github.com/Lockey-J/DdddOcrSharp");
            Trace.WriteLine($"本项目仅作为移植项目未经过大量测试 生产环境谨慎使用");
            Trace.WriteLine($"请勿违反所在地区法律法规 合理合法使用本项目");
#endif
            if (!Enum.IsDefined(mode))
            {
                throw new NotSupportedException($"不支持的模式:{mode}");
            }
            Mode = mode;
            Options = new DdddOcrOptions();
            var onnx_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Mode.GetDescription());
            if (!File.Exists(onnx_path))
            {
                throw new FileNotFoundException($"{mode}模式对应的模型文件不存在:{onnx_path}");
            }
            Options.Charset = Mode switch
            {
                DdddOcrMode.ClassifyOld => Global.OCR_OLD_CHARSET,
                DdddOcrMode.ClassifyBeta => Global.OCR_BETA_CHARSET,
                _ => Array.Empty<string>().ToList(),
            };
            SessionOptions = new SessionOptions();
            if (use_gpu)
            {
                SessionOptions.AppendExecutionProvider_CUDA(device_id);
            }
            else
            {
                SessionOptions.AppendExecutionProvider_CPU();
            }
            InferenceSession = new InferenceSession(File.ReadAllBytes(onnx_path), SessionOptions);
        }
        /// <summary>
        /// 使用自定义模型导入初始化方式
        /// </summary>
        /// <param name="import_onnx_path">导入模型路径</param>
        /// <param name="charsets_path">模型对应字符集路径</param>
        /// <param name="use_gpu">是否使用GPU</param>
        /// <param name="device_id">显卡GPU的ID</param>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="FileLoadException"></exception>
        public DDDDOCR(string import_onnx_path, string charsets_path, bool use_gpu = false, int device_id = 0)
        {
#if DEBUG
            Trace.WriteLine($"欢迎使用ddddocr，本项目专注带动行业内卷");
            Trace.WriteLine($"python版开发作者：https://github.com/sml2h3/ddddocr");
            Trace.WriteLine($"C#/NET版移植作者：https://github.com/Lockey-J/DdddOcrSharp");
            Trace.WriteLine($"请合理合法使用本项目 本项目未经过大量测试 生产环境谨慎使用");
#endif
            Mode = DdddOcrMode.Import;
            if (!File.Exists(import_onnx_path))
            {
                throw new FileNotFoundException($"文件不存在:{import_onnx_path}");
            }
            if (!File.Exists(charsets_path))
            {
                throw new FileNotFoundException($"文件不存在:{charsets_path}");
            }
            Options = DdddOcrOptions.FromJsonFile(charsets_path);
            if (Options == null)
            {
                throw new FileLoadException("数据格式错误");
            }
            SessionOptions = new SessionOptions();
            if (use_gpu)
            {
                SessionOptions.AppendExecutionProvider_CUDA(device_id);
            }
            else
            {
                SessionOptions.AppendExecutionProvider_CPU();
            }
            InferenceSession = new InferenceSession(File.ReadAllBytes(import_onnx_path), SessionOptions);
        }

        /// <summary>
        /// ddddocr解构函数
        /// </summary>
        public void Dispose()
        {
            SessionOptions?.Dispose();
            InferenceSession?.Dispose();
            GC.SuppressFinalize(this);
        }
        #endregion

        #region classification
        /// <summary>
        /// OCR识别函数
        /// </summary>
        /// <param name="bytes">待识别图片字节集</param>
        /// <param name="pngFix">是否修复为png图片</param>
        /// <returns>返回识别文本</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public string Classify(byte[] bytes, bool pngFix = false)
        {
            if (Mode == DdddOcrMode.Detect)
            {
                throw new InvalidOperationException("当前识别类型为目标检测");
            }
            using var image = Mat.FromImageData(bytes, ImreadModes.AnyColor);
            if (image.Width == 0 || image.Height == 0)
            {
                throw new InvalidOperationException("载入图像数据损坏或图片类型错误");
            }
            var inputs = ClassifyPrepareProcessing(image, pngFix);
            using var outputs = InferenceSession.Run(inputs);
            var predictions = outputs.First(x => x.Name == "output").Value as DenseTensor<long>;
            if (predictions == null)
            {
                return string.Empty;
            }
            var result = new StringBuilder();
            foreach (long prediction in predictions)
            {
                result.Append(Options?.Charset[(int)prediction]);
            }
            return result.ToString();
        }

        static readonly float[] mean = { 0.485f, 0.456f, 0.406f };
        static readonly float[] std = { 0.229f, 0.224f, 0.225f };
        private List<NamedOnnxValue> ClassifyPrepareProcessing(Mat image, bool pngFix = false)
        {
            if (Options == null)
                return new();

            Mat resizedImg;
            bool ownsResized = true;
            #region resize
            if (Mode == DdddOcrMode.Import)
            {
                if (Options.Resize.Width == -1)
                {
                    if (Options.Word)
                    {
                        resizedImg = image.Resize(new Size(Options.Resize.Height, Options.Resize.Height), interpolation: InterpolationFlags.Linear);
                    }
                    else
                    {
                        resizedImg = image.Resize(new Size(image.Width * Convert.ToDouble(Options.Resize.Height / (double)image.Height), Options.Resize.Height), interpolation: InterpolationFlags.Linear);
                    }
                }
                else
                {
                    resizedImg = image.Resize(new Size(Options.Resize.Width, Options.Resize.Height), interpolation: InterpolationFlags.Linear);
                }

                if (Options.Channel == 1)
                {
                    var gray = resizedImg.CvtColor(ColorConversionCodes.BGR2GRAY);
                    resizedImg.Dispose();
                    resizedImg = gray;
                }
                else
                {
                    if (pngFix)
                    {
                        var fixed_ = PngRgbaToRgbWhiteBackground(resizedImg);
                        if (!ReferenceEquals(fixed_, resizedImg))
                        {
                            resizedImg.Dispose();
                            resizedImg = fixed_;
                        }
                    }
                }
            }
            else
            {
                var tmp = image.Resize(new Size(image.Width * Convert.ToDouble(64d / image.Height), 64d), interpolation: InterpolationFlags.Linear);
                resizedImg = tmp.CvtColor(ColorConversionCodes.BGR2GRAY);
                tmp.Dispose();
            }
            #endregion

            try
            {
                #region tensor
                int channels = resizedImg.Channels();
                int height = resizedImg.Height;
                int width = resizedImg.Width;
                var tensor = new DenseTensor<float>(new int[] { 1, channels, height, width });

                bool isImport = Mode == DdddOcrMode.Import;
                bool singleChannel = channels == 1;

                if (singleChannel)
                {
                    var indexer = resizedImg.GetGenericIndexer<byte>();
                    // Import 单通道: 使用 mean=0.456 std=0.224; 其它模式: (c/255 - 0.5)/0.5
                    float m = isImport ? 0.456f : 0.5f;
                    float s = isImport ? 0.224f : 0.5f;
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            byte color = indexer[y, x];
                            tensor[0, 0, y, x] = ((color / 255f) - m) / s;
                        }
                    }
                }
                else
                {
                    var indexer = resizedImg.GetGenericIndexer<Vec3b>();
                    float m0 = mean[0], m1 = mean[1], m2 = mean[2];
                    float s0 = std[0], s1 = std[1], s2 = std[2];
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            Vec3b color = indexer[y, x];
                            tensor[0, 0, y, x] = ((color.Item0 / 255f) - m0) / s0;
                            tensor[0, 1, y, x] = ((color.Item1 / 255f) - m1) / s1;
                            tensor[0, 2, y, x] = ((color.Item2 / 255f) - m2) / s2;
                        }
                    }
                }

                return new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("input1", tensor) };
                #endregion
            }
            finally
            {
                if (ownsResized)
                {
                    resizedImg.Dispose();
                }
            }
        }

        private Mat PngRgbaToRgbWhiteBackground(Mat src)
        {
            if (src.Channels() != 4)
            {
                return src;
            }
            var whiteBackground = new Mat(src.Size(), MatType.CV_8UC3, Scalar.White);
            using var rgb = new Mat();
            Cv2.CvtColor(src, rgb, ColorConversionCodes.BGRA2BGR);
            using var alphaChannel = new Mat();
            Cv2.ExtractChannel(src, alphaChannel, 3);
            rgb.CopyTo(whiteBackground, alphaChannel);
            return whiteBackground;
        }
        #endregion

        #region detection
        /// <summary>
        /// 目标识别
        /// </summary>
        /// <param name="bytes">图片字节集</param>
        /// <returns>返回识别方框列表</returns>
        /// <exception cref="InvalidOperationException">初始识别类型错误</exception>
        public List<Rect> Detect(byte[] bytes)
        {
            if (Mode != DdddOcrMode.Detect)
            {
                throw new InvalidOperationException("当前识别类型为文字识别");
            }
            using var image = Mat.FromImageData(bytes, ImreadModes.AnyColor);
            if (image.Width == 0 || image.Height == 0)
            {
                throw new InvalidOperationException("载入图像数据损坏或图片类型错误");
            }
            var inputSize = new Size(416, 416);
            var inputs = DetectPrepareProcessing(image, inputSize);
            using var outputs = InferenceSession.Run(inputs);
            var predictions = outputs.First(x => x.Name == "output").Value as DenseTensor<float>;
            var bboxs = DetectHandleProcessing(predictions, image);
            return bboxs;
        }

        private List<NamedOnnxValue> DetectPrepareProcessing(Mat image, Size inputSize)
        {
            #region resize
            Mat paddedImg;
            int channels = image.Channels();

            if (channels == 3)
            {
                paddedImg = new Mat(inputSize, MatType.CV_8UC3, new Scalar(114, 114, 114));
            }
            else
            {
                paddedImg = new Mat(inputSize, MatType.CV_8UC1, new Scalar(114));
            }

            float ratio = Math.Min((float)inputSize.Height / image.Rows, (float)inputSize.Width / image.Cols);
            using var resizedImg = new Mat();
            Cv2.Resize(image, resizedImg, new Size((int)(image.Cols * ratio), (int)(image.Rows * ratio)), 0, 0, InterpolationFlags.Linear);

            resizedImg.CopyTo(paddedImg[new Rect(0, 0, resizedImg.Cols, resizedImg.Rows)]);
            #endregion

            try
            {
                #region tensor
                int height = paddedImg.Height;
                int width = paddedImg.Width;
                var tensor = new DenseTensor<float>(new int[] { 1, channels, height, width });

                if (channels == 1)
                {
                    var indexer = paddedImg.GetGenericIndexer<byte>();
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            tensor[0, 0, y, x] = indexer[y, x];
                        }
                    }
                }
                else
                {
                    var indexer = paddedImg.GetGenericIndexer<Vec3b>();
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            Vec3b color = indexer[y, x];
                            tensor[0, 0, y, x] = color.Item0;
                            tensor[0, 1, y, x] = color.Item1;
                            tensor[0, 2, y, x] = color.Item2;
                        }
                    }
                }

                return new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("images", tensor) };
                #endregion
            }
            finally
            {
                paddedImg.Dispose();
            }
        }


        /// <summary>
        /// 目标识别的多类别NMSBoxs并返回识别后的目标Rect坐标
        /// </summary>
        /// <param name="output">DenseTensor识别数据</param>
        /// <param name="image">识别图片</param>
        /// <returns></returns>
        private List<Rect> DetectHandleProcessing(DenseTensor<float>? output, Mat image)
        {
            return DetectHandleProcessing(output, image, new Size(416, 416));
        }

        /// <summary>
        /// 目标识别的多类别NMSBoxs并返回识别后的目标Rect坐标
        /// </summary>
        /// <param name="output">DenseTensor识别数据</param>
        /// <param name="image">识别图片</param>
        /// <param name="img_size">默认图片大小</param>
        /// <param name="p6"></param>
        /// <param name="nms_thr">nms参数</param>
        /// <param name="score_thr">score参数</param>
        /// <returns></returns>
        private List<Rect> DetectHandleProcessing(DenseTensor<float>? output, Mat image, Size img_size, bool p6 = false, float nms_thr = 0.45f, float score_thr = 0.1f)
        {
            if (output == null)
                return new();

            List<float> scoreslist = new();
            List<Rect> bboxs = new();
            List<Rect> result = new();
            List<int> grids = new();
            List<int> expanded_strides = new();
            int[] strides;
            if (!p6)
            {
                strides = new int[] { 8, 16, 32 };
            }
            else
            {
                strides = new int[] { 8, 16, 32, 64 };
            }

            int[] hsizes = strides.Select(p => (int)(img_size.Height / p)).ToArray();
            int[] wsizes = strides.Select(p => (int)(img_size.Width / p)).ToArray();

            for (int i = 0; i < strides.Length; i++)
            {
                int hsize = hsizes[i], wsize = wsizes[i], stride = strides[i];
                var grid = MakeGrid(hsize, wsize);
                var expanded_stride = Makeexpanded_stride(hsize * wsize, stride);
                grids.AddRange(grid);
                expanded_strides.AddRange(expanded_stride);
            }
            var (w, h) = (image.Width, image.Height); // image w and h
            var (xGain, yGain) = (img_size.Width / (float)w, img_size.Height / (float)h); // x, y gains
            var gain = Math.Min(xGain, yGain); // gain = resized / original

            for (int i = 0; i < output.Length / 6; i++)
            {
                float scores = output[0, i, 4] * output[0, i, 5];

                float x1 = output[0, i, 0];

                float y1 = output[0, i, 1];

                float x2 = output[0, i, 2];

                float y2 = output[0, i, 3];

                x1 = (x1 + grids[i * 2 + 0]) * expanded_strides[i];
                y1 = (y1 + grids[i * 2 + 1]) * expanded_strides[i];
                x2 = (float)(Math.Exp(x2) * expanded_strides[i]);
                y2 = (float)(Math.Exp(y2) * expanded_strides[i]);

                float x11 = (x1 - x2 / 2) / gain;
                float y11 = (y1 - y2 / 2) / gain;
                float x22 = (x1 + x2 / 2) / gain;
                float y22 = (y1 + y2 / 2) / gain;

                scoreslist.Add(scores);

                bboxs.Add(new Rect((int)x11, (int)y11, (int)(x22 - x11), (int)(y22 - y11)));

            }

            CvDnn.NMSBoxes(bboxs, scoreslist, score_thr, nms_thr, out var indices);
            for (int i = 0; i < indices.Length; i++)
            {
                int index = indices[i];
                result.Add(bboxs[index]);

            }
            return result;
        }
        private int[] MakeGrid(int a, int b) => MakeGridCore(a, b);

        internal static int[] MakeGridCore(int hsize, int wsize)
        {
            int[] ret = new int[hsize * wsize * 2];
            for (int i = 0; i < hsize; i++)
            {
                for (int j = 0; j < wsize; j++)
                {
                    int index = (i * wsize + j) * 2;
                    ret[index] = j;
                    ret[index + 1] = i;
                }
            }
            return ret;
        }

        private int[] Makeexpanded_stride(int a, int b)
        {
            int[] ret = new int[a];
            Array.Fill(ret, b);
            return ret;
        }
        #endregion

        #region slide
        /// <summary>
        /// 滑块缺口识别算法1
        /// </summary>
        /// <param name="target">缺口背景图片</param>
        /// <param name="background">背景图片</param>
        /// <returns>返回缺口左上角坐标</returns>
        public static Point Slide_Comparison(Mat target, Mat background)
        {
            int start_x = 0, start_y = 0;
            // 将图像转换为灰度图，以便进行差异计算
            using var bgGray = background.CvtColor(ColorConversionCodes.BGR2GRAY);
            using var tgGray = target.CvtColor(ColorConversionCodes.BGR2GRAY);

            // 计算灰度图像的差异
            using var diffRaw = new Mat();
            Cv2.Absdiff(bgGray, tgGray, diffRaw);

            using var difference = diffRaw.Threshold(80, 255, ThresholdTypes.Binary);
            var indexer = difference.GetGenericIndexer<byte>();
            for (var i = 0; i < difference.Width; i++)
            {
                int mcount = 0;
                for (var j = 0; j < difference.Height; j++)
                {
                    byte p = indexer[j, i];
                    if (p != 0)
                    {
                        mcount += 1;
                    }
                    if (mcount >= 5 && start_y == 0)
                    {
                        start_y = j - 5;
                    }
                }
                if (mcount > 5)
                {
                    start_x = i + 2;
                    break;
                }
            }
            return new Point(start_x, start_y);
        }

        /// <summary>
        /// 滑块位置识别算法（对齐 Python ddddocr <c>slide_match</c>）。
        /// </summary>
        /// <param name="target">滑块图片（仅包含滑块模板）</param>
        /// <param name="background">带缺口的背景图片</param>
        /// <param name="simpleTarget">
        /// 是否为简单滑块：
        /// true  -> 直接在灰度图上做模板匹配；
        /// false -> 先 Canny(50,150) 边缘检测，再模板匹配（默认，适合带花纹背景）。
        /// </param>
        /// <returns>匹配结果：包含滑块在背景中的矩形、中心点与置信度</returns>
        /// <exception cref="ArgumentNullException">入参为 null 时抛出</exception>
        /// <exception cref="InvalidOperationException">背景小于滑块无法匹配时抛出</exception>
        public static SlideMatchResult SlideMatch(Mat target, Mat background, bool simpleTarget = false)
        {
            ArgumentNullException.ThrowIfNull(target);
            ArgumentNullException.ThrowIfNull(background);

            if (target.Empty() || background.Empty())
            {
                throw new InvalidOperationException("target / background 图像数据为空");
            }
            if (background.Width < target.Width || background.Height < target.Height)
            {
                throw new InvalidOperationException("背景图尺寸小于滑块，无法进行模板匹配");
            }

            // Python 先将图像转为 RGB numpy 数组；OpenCvSharp 读入的是 BGR，通道顺序在灰度化后等价，
            // 所以直接 BGR->灰度即可，不必先 BGR2RGB。
            using var targetGray = new Mat();
            using var backgroundGray = new Mat();
            Cv2.CvtColor(target, targetGray, ColorConversionCodes.BGR2GRAY);
            Cv2.CvtColor(background, backgroundGray, ColorConversionCodes.BGR2GRAY);

            using var res = new Mat();
            if (simpleTarget)
            {
                // Python: cv2.matchTemplate(background_gray, target_gray, TM_CCOEFF_NORMED)
                Cv2.MatchTemplate(backgroundGray, targetGray, res, TemplateMatchModes.CCoeffNormed);
            }
            else
            {
                // Python: Canny(50,150) 后再模板匹配
                using var targetEdges = new Mat();
                using var backgroundEdges = new Mat();
                Cv2.Canny(targetGray, targetEdges, 50, 150);
                Cv2.Canny(backgroundGray, backgroundEdges, 50, 150);
                Cv2.MatchTemplate(backgroundEdges, targetEdges, res, TemplateMatchModes.CCoeffNormed);
            }

            Cv2.MinMaxLoc(res, out _, out double maxVal, out _, out Point maxLoc);

            var rect = new Rect(maxLoc.X, maxLoc.Y, target.Width, target.Height);
            return new SlideMatchResult(rect, maxVal);
        }
        #endregion
    }
}
