using Shared.MSI;
using Server.Filters;
using System.Drawing;

public class InvertFilter : IFilter
{
    public FilterType Name => FilterType.Invert;

    public Bitmap Apply(Bitmap image, Dictionary<string, object>? parameters)
    {
        var fast = new FastBitmap(image);
        var b = fast.Buffer;

        Parallel.For(0, fast.Height, y =>
        {
            int row = y * fast.Stride;

            for (int x = 0; x < fast.Width; x++)
            {
                int i = row + x * 3;

                b[i] = (byte)(255 - b[i]);
                b[i + 1] = (byte)(255 - b[i + 1]);
                b[i + 2] = (byte)(255 - b[i + 2]);
            }
        });

        return fast.ToBitmap();
    }
}