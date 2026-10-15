using System.Diagnostics;
using System.Drawing;
using Server.Core;
using Server.Filters;
using Shared.DTOs;

namespace Server.Services;

public class FilterPipeline
{
    public (Bitmap result, List<FilterTimingEntry> timings) Apply(Bitmap image, List<FilterAction> filters)
    {
        Bitmap current = image;
        var timings = new List<FilterTimingEntry>();

        foreach (var f in filters)
        {
            var sw = Stopwatch.StartNew();
            IFilter filter = FilterFactory.Create(f.Filter);
            current = filter.Apply(current, f.Parameters);
            sw.Stop();

            timings.Add(new FilterTimingEntry
            {
                Filter = f.Filter,
                DurationMs = sw.ElapsedMilliseconds
            });

            Logger.Info($"Filter {f.Filter}: {sw.ElapsedMilliseconds} ms");
        }

        return (current, timings);
    }
}
