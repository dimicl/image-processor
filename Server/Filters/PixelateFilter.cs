using System.Drawing;
using Shared.MSI;

namespace Server.Filters
{
    public class PixelateFilter : IFilter
    {
        public FilterType Name => FilterType.Pixelate;

        public Bitmap Apply(Bitmap source, Dictionary<string, object>? parameters = null)
        {
            int pixel = 10;

            if (parameters != null && parameters.TryGetValue("pixel", out var value))
            {
                if (value is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.Number)
                    pixel = je.GetInt32();
                else if (value is int i)
                    pixel = i;
                else if (value is string s && int.TryParse(s, out var parsed))
                    pixel = parsed;
            }

            pixel = Math.Max(1, pixel); 

            var fast = new FastBitmap(source);
            var b = fast.Buffer;
            int stride = fast.Stride;
            int w = fast.Width;
            int h = fast.Height;

//uzima blok i zameni ga jednom bojom, sabira boje u bloku i deli ukupnim pikselima u tom bloku i dobija boju kojom menja
            Parallel.For(0, h / pixel, py =>
            {
                for (int px = 0; px < w / pixel; px++)
                {
                    int sumB = 0, sumG = 0, sumR = 0;
                    int count = 0;

                    for (int y = 0; y < pixel; y++)
                    {
                        int yy = py * pixel + y;
                        if (yy >= h) break;

                        int row = yy * stride;

                        for (int x = 0; x < pixel; x++)
                        {
                            int xx = px * pixel + x;
                            if (xx >= w) break;

                            int i = row + xx * 3;

                            sumB += b[i];
                            sumG += b[i + 1];
                            sumR += b[i + 2];
                            count++;
                        }
                    }

                    byte br = (byte)(sumB / count);
                    byte bg = (byte)(sumG / count);
                    byte bb = (byte)(sumR / count);

                    for (int y = 0; y < pixel; y++)
                    {
                        int yy = py * pixel + y;
                        if (yy >= h) break;

                        int row = yy * stride;

                        for (int x = 0; x < pixel; x++)
                        {
                            int xx = px * pixel + x;
                            if (xx >= w) break;

                            int i = row + xx * 3;

                            b[i] = bb;
                            b[i + 1] = bg;
                            b[i + 2] = br;
                        }
                    }
                }
            });

            return fast.ToBitmap();
        }
    }
}