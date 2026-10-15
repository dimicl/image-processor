using System.Drawing;
using Shared.MSI;

namespace Server.Filters
{
    public class ColorizeGrayscaleFilter : IFilter
    {
        public FilterType Name => FilterType.Colorize;

        public Bitmap Apply(Bitmap source, Dictionary<string, object>? parameters = null)
        {
            int tr = 255;
            int tg = 150;
            int tb = 80;

            if (parameters != null)
            {
                tr = ReadInt(parameters, "r", tr);
                tg = ReadInt(parameters, "g", tg);
                tb = ReadInt(parameters, "b", tb);
            }

            var fast = new FastBitmap(source);

            var b = fast.Buffer;
            int w = fast.Width;
            int h = fast.Height;
            int stride = fast.Stride;

            byte[] result = new byte[b.Length];

            for (int y = 0; y < h; y++)
            {
                int row = y * stride;

                for (int x = 0; x < w; x++)
                {
                    int i = row + x * 3;

                    byte blue = b[i];
                    byte green = b[i + 1];
                    byte red = b[i + 2];

                    int gray = (red + green + blue) / 3;

                    byte r = (byte)Math.Clamp(gray * tr / 255, 0, 255);
                    byte g = (byte)Math.Clamp(gray * tg / 255, 0, 255);
                    byte b2 = (byte)Math.Clamp(gray * tb / 255, 0, 255);

                    result[i] = b2;
                    result[i + 1] = g;
                    result[i + 2] = r;
                }
            }

            fast.Buffer = result;
            return fast.ToBitmap();
        }

        static int ReadInt(Dictionary<string, object> p, string key, int def)
        {
            if (!p.TryGetValue(key, out var value))
                return def;

            if (value is System.Text.Json.JsonElement je)
            {
                if (je.ValueKind == System.Text.Json.JsonValueKind.Number)
                    return je.GetInt32();
                if (je.ValueKind == System.Text.Json.JsonValueKind.String &&
                    int.TryParse(je.GetString(), out var parsed))
                    return parsed;
            }

            if (value is int i) return i;

            return def;
        }
    }
}