using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using Server.Filters;

namespace Tests;

public class GoldenDatasetBenchmarkTests
{
    private static readonly string OutputCsv =
        Path.Combine(AppContext.BaseDirectory, "benchmark_results.csv");

    private static readonly string GoldenDir =
        Path.Combine(AppContext.BaseDirectory, "golden");

    [Fact]
    public void Generate_Benchmark_Report()
    {
        PlotGenerator.Generate();

        Assert.True(File.Exists(
            Path.Combine(AppContext.BaseDirectory, "benchmark_report.png")));
    }

    [Fact]
    public void Run_GoldenDataset_Benchmark()
    {
        Directory.CreateDirectory(GoldenDir);

        var images = GenerateGoldenDataset();

        var filters = new List<(string Name, IFilter Filter)>
        {
            ("Invert", new InvertFilter()),
            ("Brightness", new BrightnessFilter()),
            ("Pixelate", new PixelateFilter()),
            ("EdgeEnhance", new EdgeEnhanceFilter()),
            ("Colorize", new ColorizeGrayscaleFilter()),
            ("TimeWarp", new TimeWarpFilter())
        };

        var results = new List<string>();
        results.Add("Filter,Image,Run,Ms");

        foreach (var (imgName, img) in images)
        {
            foreach (var (filterName, filter) in filters)
            {
                for (int run = 0; run < 10; run++)
                {
                    using var clone = (Bitmap)img.Clone();

                    var sw = Stopwatch.StartNew();
                    using var result = filter.Apply(clone, GetParams(filterName));
                    sw.Stop();

                    results.Add($"{filterName},{imgName},{run},{sw.ElapsedMilliseconds}");
                }
            }
        }

        File.WriteAllLines(OutputCsv, results);
    }

    private static Dictionary<string, object>? GetParams(string filter)
    {
        return filter switch
        {
            "Brightness" => new() { ["amount"] = 30 },
            "Pixelate" => new() { ["pixel"] = 10 },
            "EdgeEnhance" => new() { ["threshold"] = 20 },
            "TimeWarp" => new() { ["factor"] = 1.5 },
            "Colorize" => new() { ["r"] = 255, ["g"] = 120, ["b"] = 80 },
            _ => null
        };
    }

    private static List<(string name, Bitmap img)> GenerateGoldenDataset()
    {
        var list = new List<(string, Bitmap)>();

        for (int i = 0; i < 12; i++)
        {
            var bmp = new Bitmap(1024, 768, PixelFormat.Format24bppRgb);

            using var g = Graphics.FromImage(bmp);

            var color = Color.FromArgb(
                (i * 40) % 255,
                (i * 80) % 255,
                (i * 120) % 255);

            g.Clear(color);

            // dodaj teksturu
            for (int x = 0; x < 1024; x += 50)
                for (int y = 0; y < 768; y += 50)
                    bmp.SetPixel(x, y, Color.White);

            list.Add(($"img_{i}", bmp));
        }

        return list;
    }
}