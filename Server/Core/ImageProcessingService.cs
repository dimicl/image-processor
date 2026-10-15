using System.Drawing;
using Shared.DTOs;

namespace Server.Services;

public class ImageProcessingService
{
    private readonly FilterPipeline _pipeline = new();

    public (Bitmap result, List<FilterTimingEntry> timings) Process(Bitmap image, List<FilterAction> filters)
    {
        return _pipeline.Apply(image, filters);
    }
}
