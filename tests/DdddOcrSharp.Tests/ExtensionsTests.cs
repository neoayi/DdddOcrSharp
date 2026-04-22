using System.ComponentModel;
using DdddOcrSharp;
using Xunit;

namespace DdddOcrSharp.Tests;

public class ExtensionsTests
{
    public enum SampleEnum
    {
        [Description("desc-a")]
        A,
        B
    }

    [Fact]
    public void GetDescription_ReturnsAttribute_WhenDefined()
    {
        Assert.Equal("desc-a", SampleEnum.A.GetDescription());
    }

    [Fact]
    public void GetDescription_ReturnsName_WhenNotDefined()
    {
        Assert.Equal("B", SampleEnum.B.GetDescription());
    }

    [Fact]
    public void DdddOcrMode_Description_PointsToOnnxFile()
    {
        Assert.Equal("onnxs\\common_old.onnx", DdddOcrMode.ClassifyOld.GetDescription());
        Assert.Equal("onnxs\\common.onnx", DdddOcrMode.ClassifyBeta.GetDescription());
        Assert.Equal("onnxs\\common_det.onnx", DdddOcrMode.Detect.GetDescription());
    }

    [Fact]
    public void ThrowIfNull_Throws_WhenNull()
    {
        object? value = null;
        Assert.Throws<ArgumentNullException>(() => DdddOcrExtensions.ThrowIfNull(value, "value"));
    }

    [Fact]
    public void ThrowIfNull_DoesNotThrow_WhenNotNull()
    {
        DdddOcrExtensions.ThrowIfNull("ok", "value");
    }
}
