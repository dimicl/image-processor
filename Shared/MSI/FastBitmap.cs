using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Shared.MSI;

public class FastBitmap
{
    public byte[] Buffer;
    public int Width;
    public int Height;
    public int Stride;

    public FastBitmap(Bitmap bmp)
    {
        Width = bmp.Width;
        Height = bmp.Height;

        var rect = new Rectangle(0, 0, Width, Height);

        var data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

        Stride = data.Stride;
        Buffer = new byte[Stride * Height];

        Marshal.Copy(data.Scan0, Buffer, 0, Buffer.Length);

        bmp.UnlockBits(data);
    }

    public Bitmap ToBitmap()
    {
        Bitmap bmp = new Bitmap(Width, Height, PixelFormat.Format24bppRgb);

        var rect = new Rectangle(0, 0, Width, Height);

        var data = bmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

        Marshal.Copy(Buffer, 0, data.Scan0, Buffer.Length);

        bmp.UnlockBits(data);

        return bmp;
    }
}
