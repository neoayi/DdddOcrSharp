using DdddOcrSharp;
using OpenCvSharp;
using Xunit;

namespace DdddOcrSharp.Tests;

public class SlideTests
{
    [Fact]
    public void Slide_Comparison_DoesNotMutateInputs()
    {
        using var target = new Mat(new Size(40, 40), MatType.CV_8UC3, Scalar.White);
        using var background = new Mat(new Size(40, 40), MatType.CV_8UC3, Scalar.White);
        Cv2.Rectangle(background, new Rect(10, 12, 8, 8), Scalar.Black, -1);

        int targetChannelsBefore = target.Channels();
        int bgChannelsBefore = background.Channels();

        var p = DDDDOCR.Slide_Comparison(target, background);

        // 入参 Mat 通道数应保持不变（原实现会原地转灰度）
        Assert.Equal(targetChannelsBefore, target.Channels());
        Assert.Equal(bgChannelsBefore, background.Channels());

        // 应当在差异区域附近找到点
        Assert.True(p.X >= 0);
        Assert.True(p.Y >= 0);
    }

    [Fact]
    public async Task Slide_ComparisonAsync_RespectsCancellation()
    {
        using var a = new Mat(new Size(10, 10), MatType.CV_8UC3, Scalar.White);
        using var b = new Mat(new Size(10, 10), MatType.CV_8UC3, Scalar.White);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => DDDDOCR.Slide_ComparisonAsync(a, b, cts.Token));
    }

    [Fact]
    public async Task SlideMatchAsync_RespectsCancellation()
    {
        using var a = new Mat(new Size(20, 20), MatType.CV_8UC3, Scalar.White);
        using var b = new Mat(new Size(40, 40), MatType.CV_8UC3, Scalar.White);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => DDDDOCR.SlideMatchAsync(a, b, cancellationToken: cts.Token));
    }

    [Fact]
    public void SlideMatch_Simple_LocatesSyntheticTarget()
    {
        // 构造 200x200 灰色背景
        using var background = new Mat(new Size(200, 200), MatType.CV_8UC3, new Scalar(200, 200, 200));
        // 在 (80,50) 处粘贴一个带纹理的 30x30 模板（避免 TM_CCOEFF_NORMED 在常量区域退化）
        using var target = new Mat(new Size(30, 30), MatType.CV_8UC3, Scalar.White);
        Cv2.Rectangle(target, new Rect(0, 0, 15, 30), new Scalar(0, 0, 0), -1);
        Cv2.Circle(target, new Point(22, 15), 5, new Scalar(0, 0, 255), -1);

        // 把 target 贴到 background
        var roi = new Rect(80, 50, 30, 30);
        target.CopyTo(background[roi]);

        var r = DDDDOCR.SlideMatch(target, background, simpleTarget: true);

        Assert.InRange(r.Target.X, 78, 82);
        Assert.InRange(r.Target.Y, 48, 52);
        Assert.Equal(30, r.Target.Width);
        Assert.Equal(30, r.Target.Height);
        Assert.True(r.Confidence > 0.9, $"confidence={r.Confidence}");
    }

    [Fact]
    public void SlideMatch_ThrowsOnNull()
    {
        using var m = new Mat(new Size(10, 10), MatType.CV_8UC3, Scalar.White);
        Assert.Throws<ArgumentNullException>(() => DDDDOCR.SlideMatch(null!, m));
        Assert.Throws<ArgumentNullException>(() => DDDDOCR.SlideMatch(m, null!));
    }

    [Fact]
    public void SlideMatch_ThrowsWhenBackgroundSmallerThanTarget()
    {
        using var target = new Mat(new Size(50, 50), MatType.CV_8UC3, Scalar.White);
        using var bg = new Mat(new Size(20, 20), MatType.CV_8UC3, Scalar.White);
        Assert.Throws<InvalidOperationException>(() => DDDDOCR.SlideMatch(target, bg));
    }
}
