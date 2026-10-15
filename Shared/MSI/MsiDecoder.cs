using Shared.Enums;
using Shared.MSI;
using Shared.Utils;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

namespace Shared.MSI;

public static class MsiDecoder
{
    public static Bitmap Decode(byte[] data)
    {
        if (data.Length < 32)
            throw new InvalidDataException("MSI file too small");

        uint storedCrc = BitConverter.ToUInt32(data, data.Length - 4);
        uint calcCrc = Crc32.Compute(data.AsSpan(0, data.Length - 4));
        if (storedCrc != calcCrc)
            throw new InvalidDataException("CRC32 mismatch");

        using var ms = new MemoryStream(data);
        using var br = new BinaryReader(ms);

        var magic = Encoding.ASCII.GetString(br.ReadBytes(4));
        if (magic != MsiConstants.MAGIC)
            throw new InvalidDataException("Invalid MSI magic");

        br.ReadUInt16(); 
        br.ReadUInt16(); 

        uint width = br.ReadUInt32();
        uint height = br.ReadUInt32();
        byte channels = br.ReadByte();
        var colorSpace = (ColorSpace)br.ReadByte();
        var compression = (CompressionType)br.ReadByte();
        br.ReadByte(); // reserved

        int metaLen = br.ReadInt32();
        int pixelLen = br.ReadInt32();
        byte[] meta = br.ReadBytes(metaLen);
        byte[] pixelSegment = br.ReadBytes(pixelLen);

        byte[] hsvPixels;
        int stride = ((int)width * (int)channels + 3) & ~3;

        if (compression == CompressionType.None)
        {
            hsvPixels = pixelSegment;
        }
        else if (compression == CompressionType.Huffman)
        {
            int expectedLen = stride * (int)height;
            hsvPixels = HuffmanCodec.Decompress(pixelSegment, meta, expectedLen);
        }
        else
        {
            throw new NotSupportedException($"Compression type {compression} is not supported");
        }

        if (colorSpace != ColorSpace.HSV && colorSpace != ColorSpace.RGB)
            throw new NotSupportedException($"Colorspace {colorSpace} is not supported");

        byte[] rgbPixels = colorSpace == ColorSpace.HSV
            ? HsvColor.HsvBufferToRgb(hsvPixels, (int)width, (int)height, stride)
            : hsvPixels;

        var bmp = new Bitmap((int)width, (int)height, PixelFormat.Format24bppRgb);
        var fast = new FastBitmap(bmp);
        int copyLen = Math.Min(rgbPixels.Length, fast.Buffer.Length);
        Buffer.BlockCopy(rgbPixels, 0, fast.Buffer, 0, copyLen);
        return fast.ToBitmap();
    }
}
