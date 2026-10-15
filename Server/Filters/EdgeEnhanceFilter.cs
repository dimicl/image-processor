using System.Drawing;
using Shared.MSI;

namespace Server.Filters;

public class EdgeEnhanceFilter : IFilter
{
    public FilterType Name => FilterType.EdgeEnhance;

   //pojacava ivice odnosno konture
    public Bitmap Apply(Bitmap source, Dictionary<string, object>? parameters = null)
    {
        int threshold = 20;

        if (parameters != null && parameters.TryGetValue("threshold", out var value))
        {
            if (value is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.Number)
                threshold = je.GetInt32();
            else if (value is int i)
                threshold = i;
            else if (value is string s && int.TryParse(s, out var parsed))
                threshold = parsed;
        }

        var fast = new FastBitmap(source);
        var b = fast.Buffer;
        int w = fast.Width, h = fast.Height, stride = fast.Stride;
        byte[] result = new byte[b.Length];

        for (int y = 1; y < h - 1; y++)
        {
            for (int x = 1; x < w - 1; x++)
            {
                //preko inteziteta  
                byte a = Gray(b, stride, x - 1, y - 1);
                byte bb = Gray(b, stride, x, y - 1);
                byte c = Gray(b, stride, x + 1, y - 1);
                byte d = Gray(b, stride, x - 1, y);
                byte e = Gray(b, stride, x, y);
                byte f = Gray(b, stride, x + 1, y);
                byte g = Gray(b, stride, x - 1, y + 1);
                byte hh = Gray(b, stride, x, y + 1);
                byte i = Gray(b, stride, x + 1, y + 1);

                int v1 = Math.Abs(c - g);
                int v2 = Math.Abs(i - a);
                int v3 = Math.Abs(hh - bb);
                int v4 = Math.Abs(f - d);
                int pixelMax = Math.Max(Math.Max(v1, v2), Math.Max(v3, v4));

                byte m = e;
                if (pixelMax > threshold && pixelMax > e)
                    m = (byte)Math.Max(e, pixelMax);

                int idx = y * stride + x * 3;
                result[idx] = result[idx + 1] = result[idx + 2] = m;
            }
        }

        fast.Buffer = result;
        return fast.ToBitmap();
    }

    private static byte Gray(byte[] b, int stride, int x, int y)
    {
        int i = y * stride + x * 3;
        return (byte)(0.299 * b[i + 2] + 0.587 * b[i + 1] + 0.114 * b[i]);
    }
}
