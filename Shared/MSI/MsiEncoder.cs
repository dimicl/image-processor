using Shared.Enums;
using Shared.MSI;
using Shared.Utils;
using System.Drawing;
using System.Text;

namespace Shared.MSI;

public static class MsiEncoder
{
    public const byte DefaultChannels = 3;
    public const ColorSpace DefaultColorSpace = ColorSpace.HSV;
    public const CompressionType DefaultCompression = CompressionType.Huffman;

    public static byte[] Encode(Bitmap bmp, byte[]? meta = null)
    {
        var fast = new FastBitmap(bmp);
        byte[] hsvPixels = HsvColor.RgbBufferToHsv(fast.Buffer, fast.Width, fast.Height, fast.Stride);

        var (compressed, huffMeta) = HuffmanCodec.Compress(hsvPixels);
        if (meta == null || meta.Length == 0 || Encoding.UTF8.GetString(meta) == "{}")
            meta = huffMeta;

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        bw.Write(Encoding.ASCII.GetBytes(MsiConstants.MAGIC));
        bw.Write(MsiConstants.VERSION);
        bw.Write((ushort)28);
        bw.Write((uint)bmp.Width);
        bw.Write((uint)bmp.Height);
        bw.Write(DefaultChannels);
        bw.Write((byte)DefaultColorSpace);
        bw.Write((byte)DefaultCompression);
        bw.Write((byte)0); // reserved
        bw.Write(meta.Length);
        bw.Write(compressed.Length);
        bw.Write(meta);
        bw.Write(compressed);

        var raw = ms.ToArray();
        bw.Write(Crc32.Compute(raw));
        bw.Flush();
        return ms.ToArray();
    }
}
