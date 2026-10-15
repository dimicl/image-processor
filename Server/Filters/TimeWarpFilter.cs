using System.Drawing;
using Shared.MSI;

namespace Server.Filters
{
    public class TimeWarpFilter : IFilter
    {
        public FilterType Name => FilterType.TimeWarp;

        public Bitmap Apply(Bitmap source, Dictionary<string, object>? parameters = null)
        {
            double factor = 1.5;

            if (parameters != null && parameters.TryGetValue("factor", out var value))
            {
                if (value is System.Text.Json.JsonElement je)
                {
                    if (je.ValueKind == System.Text.Json.JsonValueKind.Number)
                        factor = je.GetDouble();
                    else if (je.ValueKind == System.Text.Json.JsonValueKind.String &&
                            double.TryParse(je.GetString(), out var parsed))
                        factor = parsed;
                }
                else if (value is double d)
                {
                    factor = d;
                }
                else if (value is float f)
                {
                    factor = f;
                }
            }

            var fast = new FastBitmap(source);

            int w = fast.Width;
            int h = fast.Height;
            int stride = fast.Stride;
            var b = fast.Buffer;

            var result = new byte[b.Length];

            int midX = w / 2;
            int midY = h / 2;

            Parallel.For(0, h, y =>
            {
                for (int x = 0; x < w; x++)
                {
                    int tx = x - midX;
                    int ty = y - midY;

                    double theta = Math.Atan2(ty, tx);
                    double radius = Math.Sqrt(tx * tx + ty * ty);
                    double nr = Math.Sqrt(radius) * factor;

                    int sx = (int)(midX + nr * Math.Cos(theta));
                    int sy = (int)(midY + nr * Math.Sin(theta));

                    int i = y * stride + x * 3;

                    if ((uint)sx < (uint)w && (uint)sy < (uint)h)
                    {
                        int si = sy * stride + sx * 3;

                        result[i] = b[si];
                        result[i + 1] = b[si + 1];
                        result[i + 2] = b[si + 2];
                    }
                }
            });

            fast.Buffer = result;
            return fast.ToBitmap();
        }
    }
}