namespace DdddOcrSharp
{
    /// <summary>
    /// SlideMatch 匹配结果，结构对齐 Python ddddocr <c>slide_match</c> 返回字典。
    /// </summary>
    public readonly struct SlideMatchResult
    {
        /// <summary>
        /// 滑块在背景中的匹配矩形区域（左上角坐标 + 滑块宽高）。
        /// </summary>
        public OpenCvSharp.Rect Target { get; }

        /// <summary>
        /// 滑块中心点 X 坐标（= Target.X + Target.Width / 2）。
        /// </summary>
        public int TargetX { get; }

        /// <summary>
        /// 滑块中心点 Y 坐标（= Target.Y + Target.Height / 2）。
        /// </summary>
        public int TargetY { get; }

        /// <summary>
        /// 模板匹配的置信度（<c>cv2.minMaxLoc</c> 返回的 maxVal，范围 [-1, 1]）。
        /// </summary>
        public double Confidence { get; }

        /// <summary>
        /// 创建 SlideMatch 匹配结果。
        /// </summary>
        public SlideMatchResult(OpenCvSharp.Rect target, double confidence)
        {
            Target = target;
            TargetX = target.X + target.Width / 2;
            TargetY = target.Y + target.Height / 2;
            Confidence = confidence;
        }
    }
}
