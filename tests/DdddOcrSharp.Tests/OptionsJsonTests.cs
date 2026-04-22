using System.Text.Json;
using DdddOcrSharp;
using OpenCvSharp;
using Xunit;

namespace DdddOcrSharp.Tests;

public class OptionsJsonTests
{
    [Fact]
    public void JsonRoundTrip_PreservesAllFields()
    {
        var opts = new DdddOcrOptions
        {
            Charset = new List<string> { "a", "b", "中" },
            Word = true,
            Resize = new Size(64, 96),
            Channel = 3
        };

        string json = opts.ToJson();
        var back = DdddOcrOptions.FromJson(json);

        Assert.NotNull(back);
        Assert.Equal(opts.Charset, back!.Charset);
        Assert.Equal(opts.Word, back.Word);
        Assert.Equal(opts.Resize.Width, back.Resize.Width);
        Assert.Equal(opts.Resize.Height, back.Resize.Height);
        Assert.Equal(opts.Channel, back.Channel);
    }

    [Fact]
    public void Size_SerializedAs_WidthHeightArray()
    {
        var opts = new DdddOcrOptions { Resize = new Size(10, 20) };
        string json = opts.ToJson();
        Assert.Contains("[10,20]", json);
    }

    [Theory]
    [InlineData("{\"Resize\":\"bad\"}")]
    [InlineData("{\"Resize\":[1]}")]
    [InlineData("{\"Resize\":[1,\"x\"]}")]
    [InlineData("{\"Resize\":[1,2,3]}")]
    public void InvalidSizeJson_Throws(string json)
    {
        Assert.Throws<JsonException>(() => DdddOcrOptions.FromJson(json));
    }
}
