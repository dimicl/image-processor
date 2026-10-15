using System.Drawing;

namespace Server.Filters
{
    public interface IFilter
    {
        FilterType Name { get; }

        Bitmap Apply(Bitmap image, Dictionary<string, object>? parameters);

    }
}
