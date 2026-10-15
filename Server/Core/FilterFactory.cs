using Server.Filters;
using Shared.DTOs;

namespace Server.Core
{
    public static class FilterFactory
    {
        public static IFilter Create(FilterType type)
        {
            return type switch
            {
                FilterType.Invert => new InvertFilter(),
                FilterType.Brightness => new BrightnessFilter(),
                FilterType.EdgeEnhance => new EdgeEnhanceFilter(),
                FilterType.EmbossLaplacian => new EmbossLaplacianFilter(),
                FilterType.Pixelate => new PixelateFilter(),
                FilterType.TimeWarp => new TimeWarpFilter(),
                FilterType.JjnDithering => new JjnDitheringFilter(),
                FilterType.Colorize => new ColorizeGrayscaleFilter(),

                _ => throw new Exception("Unknown filter type")
            };
        }
    }
}