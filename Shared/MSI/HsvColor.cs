namespace Shared.MSI;

public static class HsvColor
{
    public static (byte h, byte s, byte v) RgbToHsv(byte r, byte g, byte b)
    {
        double rd = r / 255.0;
        double gd = g / 255.0;
        double bd = b / 255.0;

        double max = Math.Max(rd, Math.Max(gd, bd));
        double min = Math.Min(rd, Math.Min(gd, bd));
        double delta = max - min;

        double h = 0;
        if (delta > 0.00001)
        {
            if (max == rd)
                h = 60 * (((gd - bd) / delta) % 6);
            else if (max == gd)
                h = 60 * (((bd - rd) / delta) + 2);
            else
                h = 60 * (((rd - gd) / delta) + 4);
        }
        if (h < 0) h += 360;

        double s = max == 0 ? 0 : delta / max;
        double v = max;

        return ((byte)(h / 360.0 * 255), (byte)(s * 255), (byte)(v * 255));
    }

    public static (byte r, byte g, byte b) HsvToRgb(byte h, byte s, byte v)
    {
        double hd = h / 255.0 * 360.0;
        double sd = s / 255.0;
        double vd = v / 255.0;

        double c = vd * sd;
        double x = c * (1 - Math.Abs((hd / 60.0 % 2) - 1));
        double m = vd - c;

        double r1, g1, b1;
        if (hd < 60) { r1 = c; g1 = x; b1 = 0; }
        else if (hd < 120) { r1 = x; g1 = c; b1 = 0; }
        else if (hd < 180) { r1 = 0; g1 = c; b1 = x; }
        else if (hd < 240) { r1 = 0; g1 = x; b1 = c; }
        else if (hd < 300) { r1 = x; g1 = 0; b1 = c; }
        else { r1 = c; g1 = 0; b1 = x; }

        return ((byte)((r1 + m) * 255), (byte)((g1 + m) * 255), (byte)((b1 + m) * 255));
    }

    public static byte[] RgbBufferToHsv(byte[] rgb, int width, int height, int stride)
    {
        var hsv = new byte[stride * height];
        for (int y = 0; y < height; y++)
        {
            int row = y * stride;
            for (int x = 0; x < width; x++)
            {
                int i = row + x * 3;
                var (h, s, v) = RgbToHsv(rgb[i + 2], rgb[i + 1], rgb[i]);
                hsv[i] = h;
                hsv[i + 1] = s;
                hsv[i + 2] = v;
            }
        }
        return hsv;
    }

    public static byte[] HsvBufferToRgb(byte[] hsv, int width, int height, int stride)
    {
        var rgb = new byte[stride * height];
        for (int y = 0; y < height; y++)
        {
            int row = y * stride;
            for (int x = 0; x < width; x++)
            {
                int i = row + x * 3;
                var (r, g, b) = HsvToRgb(hsv[i], hsv[i + 1], hsv[i + 2]);
                rgb[i] = b;
                rgb[i + 1] = g;
                rgb[i + 2] = r;
            }
        }
        return rgb;
    }
}
