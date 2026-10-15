using Server.Core;
using Server.Filters;
using System.Drawing;
using System.Drawing.Imaging;

namespace Tests;

public class FilterTests
{
    [Fact]
    public void Invert_ChangesPixels()
    {
        using var bmp = SolidBitmap(Color.White);
        var filter = new InvertFilter();
        using var result = filter.Apply(bmp, null);
        var c = result.GetPixel(0, 0);
        Assert.True(c.R < 10 && c.G < 10 && c.B < 10);
    }

    [Fact]
    public void Brightness_IncreasesValue()
    {
        using var bmp = SolidBitmap(Color.FromArgb(100, 100, 100));
        var filter = new BrightnessFilter();
        using var result = filter.Apply(bmp, new Dictionary<string, object> { ["amount"] = 50 });
        Assert.True(result.GetPixel(0, 0).R > 100);
    }

    [Fact]
    public void Pixelate_ReducesUniqueColors()
    {
        using var bmp = GradientBitmap(80, 80);
        var filter = new PixelateFilter();
        using var result = filter.Apply(bmp, new Dictionary<string, object> { ["pixel"] = 10 });
        var colors = new HashSet<int>();
        for (int y = 0; y < result.Height; y += 5)
            for (int x = 0; x < result.Width; x += 5)
            {
                var p = result.GetPixel(x, y);
                colors.Add(p.ToArgb());
            }
        Assert.True(colors.Count < 200);
    }

    [Fact]
    public void EmbossLaplacian_ProducesGrayscale()
    {
        using var bmp = GradientBitmap(32, 32);
        var filter = new EmbossLaplacianFilter();
        using var result = filter.Apply(bmp, null);
        var p = result.GetPixel(16, 16);
        Assert.Equal(p.R, p.G);
        Assert.Equal(p.G, p.B);
    }

    [Fact]
    public void EdgeEnhance_RunsOnGradient()
    {
        using var bmp = GradientBitmap(40, 40);
        var filter = new EdgeEnhanceFilter();
        using var result = filter.Apply(bmp, new Dictionary<string, object> { ["threshold"] = 10 });
        Assert.Equal(40, result.Width);
    }

    [Fact]
    public void TimeWarp_ChangesImage()
    {
        using var bmp = GradientBitmap(50, 50);
        var filter = new TimeWarpFilter();
        using var result = filter.Apply(bmp, new Dictionary<string, object> { ["factor"] = 1.5 });
        bool anyDifferent = false;
        for (int y = 0; y < 50 && !anyDifferent; y += 5)
            for (int x = 0; x < 50 && !anyDifferent; x += 5)
                if (bmp.GetPixel(x, y).ToArgb() != result.GetPixel(x, y).ToArgb())
                    anyDifferent = true;
        Assert.True(anyDifferent);
    }

    [Fact]
    public void JjnDithering_ProducesBinaryTones()
    {
        using var bmp = GradientBitmap(30, 30);
        var filter = new JjnDitheringFilter();
        using var result = filter.Apply(bmp, null);
        var p = result.GetPixel(5, 5);
        Assert.True(p.R == 0 || p.R == 255);
    }

    [Fact]
    public void Colorize_AppliesTint()
    {
        using var bmp = SolidBitmap(Color.Gray);
        var filter = new ColorizeGrayscaleFilter();
        using var result = filter.Apply(bmp, new Dictionary<string, object>
        {
            ["r"] = 255, ["g"] = 0, ["b"] = 0
        });
        Assert.True(result.GetPixel(0, 0).R > result.GetPixel(0, 0).G);
    }

    [Fact]
    public void FilterPipeline_BatchThreeFilters_Under500ms()
    {
        using var bmp = GradientBitmap(1024, 768);
        var pipeline = new Server.Services.FilterPipeline();
        var filters = new List<Shared.DTOs.FilterAction>
        {
            new() { Filter = FilterType.Brightness, Parameters = new() { ["amount"] = 10 } },
            new() { Filter = FilterType.Invert, Parameters = new() },
            new() { Filter = FilterType.EdgeEnhance, Parameters = new() { ["threshold"] = 20 } }
        };

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var (result, _) = pipeline.Apply(bmp, filters);
        sw.Stop();

        Assert.NotNull(result);
        Assert.True(sw.ElapsedMilliseconds <= 500,
            $"Chain took {sw.ElapsedMilliseconds} ms (limit 500 ms)");
    }

    [Fact]
    public void ImageConverter_RejectsOversizedUpload()
    {
        var huge = new byte[Server.Core.ImageConverter.MaxUploadBytes + 1];
        huge[0] = 0x89; huge[1] = 0x50; huge[2] = 0x4E; huge[3] = 0x47;
        Assert.Throws<ArgumentException>(() => Server.Core.ImageConverter.ValidateImageBytes(huge));
    }

    private static Bitmap SolidBitmap(Color c)
    {
        var bmp = new Bitmap(4, 4, PixelFormat.Format24bppRgb);
        using var g = Graphics.FromImage(bmp);
        g.Clear(c);
        return bmp;
    }

    private static Bitmap GradientBitmap(int w, int h)
    {
        var bmp = new Bitmap(w, h, PixelFormat.Format24bppRgb);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                bmp.SetPixel(x, y, Color.FromArgb(x * 255 / Math.Max(w - 1, 1), y * 255 / Math.Max(h - 1, 1), 128));
        return bmp;
    }
}
