using DdddOcrSharp;
using Xunit;

namespace DdddOcrSharp.Tests;

public class MakeGridTests
{
    [Fact]
    public void MakeGrid_Square_ProducesExpectedIndices()
    {
        // 2x2 grid
        var g = DDDDOCR.MakeGridCore(2, 2);
        // (y,x) pairs: (0,0),(0,1),(1,0),(1,1) stored as (x,y) per implementation
        Assert.Equal(new[] { 0, 0, 1, 0, 0, 1, 1, 1 }, g);
    }

    [Fact]
    public void MakeGrid_NonSquare_DoesNotOverflow_AndIsCorrect()
    {
        // hsize=3, wsize=5 -> 15 cells, 30 entries
        int h = 3, w = 5;
        var g = DDDDOCR.MakeGridCore(h, w);
        Assert.Equal(h * w * 2, g.Length);

        for (int i = 0; i < h; i++)
        {
            for (int j = 0; j < w; j++)
            {
                int idx = (i * w + j) * 2;
                Assert.Equal(j, g[idx]);     // x
                Assert.Equal(i, g[idx + 1]); // y
            }
        }
    }
}
