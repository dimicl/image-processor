using Shared.MSI;
using Server.Filters;
using System.Drawing;

public class BrightnessFilter : IFilter
{
    public FilterType Name => FilterType.Brightness;

    public Bitmap Apply(Bitmap image, Dictionary<string, object>? parameters)
    {
        int amount = 0;

        if (parameters != null &&
            parameters.TryGetValue("amount", out var value))
        {
            if (value is System.Text.Json.JsonElement json)
                amount = json.GetInt32();
            else
                amount = Convert.ToInt32(value);
        }

        var fast = new FastBitmap(image);
        var b = fast.Buffer;

        Parallel.For(0, fast.Height, y =>
        {
            int row = y * fast.Stride;

            for (int x = 0; x < fast.Width; x++)
            {
                int i = row + x * 3;

                b[i] = (byte)Math.Clamp(b[i] + amount, 0, 255);
                b[i + 1] = (byte)Math.Clamp(b[i + 1] + amount, 0, 255);
                b[i + 2] = (byte)Math.Clamp(b[i + 2] + amount, 0, 255);
            }
        });

        return fast.ToBitmap();
    }
}