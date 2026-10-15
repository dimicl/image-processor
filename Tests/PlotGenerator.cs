using ScottPlot;
using System.Globalization;

namespace Tests;

public class PlotGenerator
{
    private const string CsvFile = "benchmark_results.csv";
    private static readonly string OutputImage =
        Path.Combine(AppContext.BaseDirectory, "benchmark_report.png");

    public static void Generate()
    {
        if (!File.Exists(CsvFile))
        {
            Console.WriteLine("CSV not found");
            return;
        }

        var lines = File.ReadAllLines(CsvFile).Skip(1);

        var data = new Dictionary<string, List<double>>();

        foreach (var line in lines)
        {
            var parts = line.Split(',');

            string filter = parts[0];
            double ms = double.Parse(parts[3], CultureInfo.InvariantCulture);

            if (!data.ContainsKey(filter))
                data[filter] = new List<double>();

            data[filter].Add(ms);
        }

        var labels = data.Keys.ToArray();
        var values = data.Values.Select(v => v.Average()).ToArray();

        var plt = new ScottPlot.Plot();

        var bars = plt.Add.Bars(values);

        for (int i = 0; i < labels.Length; i++)
        {
            bars.Bars[i].Position = i;
            bars.Bars[i].Label = labels[i];
        }

        plt.Axes.Bottom.TickGenerator =
            new ScottPlot.TickGenerators.NumericManual(
                Enumerable.Range(0, labels.Length).Select(i => (double)i).ToArray(),
                labels);

        plt.Title("Filter Latency Benchmark (avg ms)");
        plt.YLabel("Milliseconds");

        plt.SavePng(OutputImage, 800, 500);

        Console.WriteLine($"Saved: {OutputImage}");
    }
}