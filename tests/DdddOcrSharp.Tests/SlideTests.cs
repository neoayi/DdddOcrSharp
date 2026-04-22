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
}
