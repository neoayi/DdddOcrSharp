using OpenCvSharp;

namespace DdddOcrSharp
{
    /// <summary>
    /// DDDDOCR 异步 API。
    /// 注意：OCR 推理本身是 CPU 密集型操作，异步 API 的主要价值在于释放调用线程（如 ASP.NET Core 请求线程、UI 线程），
    /// 而不是加速推理本身。内部实现使用 <see cref="System.Threading.Tasks.Task.Run(System.Action)"/> 调度，
    /// 并通过 <c>RunOptions.Terminate</c> 响应取消。
    /// 同一 <see cref="DDDDOCR"/> 实例不保证多线程并发调用安全，请勿在并发场景下共享单一实例。
    /// </summary>
    public partial class DDDDOCR
    {
        /// <summary>
        /// 异步 OCR 识别。
        /// </summary>
        /// <param name="bytes">待识别图片字节集</param>
        /// <param name="pngFix">是否修复为 png 图片</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>返回识别文本</returns>
        public Task<string> ClassifyAsync(byte[] bytes, bool pngFix = false, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Classify(bytes, pngFix);
            }, cancellationToken);
        }

        /// <summary>
        /// 异步目标检测。
        /// </summary>
        /// <param name="bytes">图片字节集</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>返回识别方框列表</returns>
        public Task<List<Rect>> DetectAsync(byte[] bytes, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Detect(bytes);
            }, cancellationToken);
        }

        /// <summary>
        /// 异步滑块缺口识别算法 1。
        /// </summary>
        public static Task<Point> Slide_ComparisonAsync(Mat target, Mat background, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Slide_Comparison(target, background);
            }, cancellationToken);
        }

        /// <summary>
        /// 异步滑块位置识别算法（对齐 Python ddddocr <c>slide_match</c>）。
        /// </summary>
        public static Task<SlideMatchResult> SlideMatchAsync(Mat target, Mat background, bool simpleTarget = false, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return SlideMatch(target, background, simpleTarget);
            }, cancellationToken);
        }
    }
}
