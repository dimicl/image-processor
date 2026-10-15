using System.Drawing;
using Shared.MSI;

namespace Server.Filters
{
    public class EmbossLaplacianFilter : IFilter
    {
        public FilterType Name => FilterType.EmbossLaplacian;

        private readonly int[,] kernel =
        {
            { -2, -1,  0 },
            { -1,  1,  1 },
            {  0,  1,  2 }
        };

        public Bitmap Apply(Bitmap source, Dictionary<string, object>? parameters = null)
        {
            var fast = new FastBitmap(source);
            var b = fast.Buffer;

            int w = fast.Width;
            int h = fast.Height;
            int stride = fast.Stride;

            byte[] result = new byte[b.Length];

            for (int y = 1; y < h - 1; y++)
            {
                for (int x = 1; x < w - 1; x++)
                {
                    int sum = 0;

                    for (int ky = -1; ky <= 1; ky++)
                    {
                        int yy = y + ky;
                        int row = yy * stride;

                        for (int kx = -1; kx <= 1; kx++)
                        {
                            int xx = x + kx;
                            int i = row + xx * 3;

                            int gray =
                                (b[i] + b[i + 1] + b[i + 2]) / 3;

                            sum += gray * kernel[ky + 1, kx + 1];
                        }
                    }

                    sum = Math.Clamp(sum + 128, 0, 255);

                    int idx = y * stride + x * 3;

                    result[idx] = (byte)sum;
                    result[idx + 1] = (byte)sum;
                    result[idx + 2] = (byte)sum;
                }
            }

            fast.Buffer = result;
            return fast.ToBitmap();
        }
    }
}