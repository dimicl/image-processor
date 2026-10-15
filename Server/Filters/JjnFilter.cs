using System.Drawing;
using Shared.MSI;

namespace Server.Filters
{
    public class JjnDitheringFilter : IFilter
    {
        public FilterType Name => FilterType.JjnDithering;

        private readonly (int dx, int dy, double f)[] kernel =
        {
            (1,0,7.0/48),(2,0,5.0/48),

            (-2,1,3.0/48),(-1,1,5.0/48),(0,1,7.0/48),(1,1,5.0/48),(2,1,3.0/48),

            (-2,2,1.0/48),(-1,2,3.0/48),(0,2,5.0/48),(1,2,3.0/48),(2,2,1.0/48)
        };

        public Bitmap Apply(Bitmap source, Dictionary<string, object>? parameters = null)
        {
            var fast = new FastBitmap(source);

            int w = fast.Width;
            int h = fast.Height;
            int stride = fast.Stride;

            var buffer = new double[w, h];
            var b = fast.Buffer;

            Parallel.For(0, h, y =>
            {
                int row = y * stride;

                for (int x = 0; x < w; x++)
                {
                    int i = row + x * 3;

                    buffer[x, y] =
                        (b[i] + b[i + 1] + b[i + 2]) / 3.0;
                }
            });

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    double oldP = buffer[x, y];
                    double newP = oldP < 128 ? 0 : 255;
                    double error = oldP - newP;

                    int i = y * stride + x * 3;

                    byte val = (byte)newP;
                    b[i] = val;
                    b[i + 1] = val;
                    b[i + 2] = val;

                    foreach (var (dx, dy, f) in kernel)
                    {
                        int nx = x + dx;
                        int ny = y + dy;

                        if ((uint)nx < (uint)w && (uint)ny < (uint)h)
                        {
                            buffer[nx, ny] += error * f;
                        }
                    }
                }
            }

            return fast.ToBitmap();
        }
    }
}