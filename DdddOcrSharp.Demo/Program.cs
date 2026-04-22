using DdddOcrSharp;
using OpenCvSharp;
using System;
using System.Text.Json;

namespace DdddOcr.Demo
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Mat ocr = new("ocr.jpg", ImreadModes.AnyColor);
            Mat det = new("det.png", ImreadModes.AnyColor);

            DDDDOCR ddddOcrDet = new(DdddOcrMode.Detect);
            DDDDOCR ddddOcrOcrOld = new(DdddOcrMode.ClassifyOld);
            DDDDOCR ddddOcrOcrNew = new(DdddOcrMode.ClassifyBeta);

            var OcrOldResult = ddddOcrOcrOld.Classify(ocr.ToBytes());
            Console.WriteLine("旧版本文本识别结果：" + OcrOldResult);
            Console.WriteLine();

            var OcrNewResult = ddddOcrOcrNew.Classify(ocr.ToBytes());
            Console.WriteLine("新版本文本识别结果：" + OcrNewResult);
            Console.WriteLine();

            var Detresult = ddddOcrDet.Detect(det.ToBytes());
            foreach (var item in Detresult)
            {
                det.Rectangle(item, new Scalar(0, 0, 255), 2);
            }
            Cv2.ImShow("det", det);
            Console.WriteLine("目标识别到的坐标为：" + JsonSerializer.Serialize(Detresult));
            Console.WriteLine();

            // 遍历所有 (tg, bg) 样本对做 SlideMatch 视觉验证
            string[] pairs = { "", "1", "2", "3", "4" };
            foreach (var suffix in pairs)
            {
                var tgPath = $"tg{suffix}.png";
                var bgPath = $"bg{suffix}.png";
                if (!System.IO.File.Exists(tgPath) || !System.IO.File.Exists(bgPath))
                    continue;

                using var tg = new Mat(tgPath, ImreadModes.AnyColor);
                using var bg = new Mat(bgPath, ImreadModes.AnyColor);

                var result = DDDDOCR.SlideMatch(tg, bg);
                Console.WriteLine($"[tg{suffix}/bg{suffix}] rect={JsonSerializer.Serialize(result.Target)} center=({result.TargetX},{result.TargetY}) conf={result.Confidence:F3}");

                using var preview = bg.Clone();
                preview.Rectangle(result.Target, new Scalar(0, 0, 255), 2);
                Cv2.ImShow($"SlideMatch-{(string.IsNullOrEmpty(suffix) ? "0" : suffix)}", preview);
            }

            Console.WriteLine();
            Cv2.WaitKey(0);
        }
    }
}
