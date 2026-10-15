using Shared.MSI;
using Shared.Utils;
using System.Drawing;
using System.Drawing.Imaging;

namespace Tests;

public class MsiCodecTests
{
    [Fact]
    public void EncodeDecode_RoundTrip_PreservesDimensions()
    {
        using var bmp = CreateTestBitmap(64, 48);
        byte[] encoded = MsiEncoder.Encode(bmp);
        using var decoded = MsiDecoder.Decode(encoded);

        Assert.Equal(64, decoded.Width);
        Assert.Equal(48, decoded.Height);
    }

    [Fact]
    public void Encode_UsesHsvAndHuffmanForIndex9()
    {
        using var bmp = CreateTestBitmap(8, 8);
        byte[] encoded = MsiEncoder.Encode(bmp);

        Assert.Equal((byte)'M', encoded[0]);
        Assert.Equal((byte)'S', encoded[1]);
        Assert.Equal((byte)'I', encoded[2]);
        Assert.Equal((byte)'0', encoded[3]);
        Assert.Equal(3, encoded[16]);
        Assert.Equal(3, encoded[17]);
        Assert.Equal(2, encoded[18]);
    }

    [Fact]
    public void Decode_RejectsCorruptedCrc()
    {
        using var bmp = CreateTestBitmap(16, 16);
        byte[] encoded = MsiEncoder.Encode(bmp);
        encoded[^2] ^= 0xFF;

        Assert.Throws<InvalidDataException>(() => MsiDecoder.Decode(encoded));
    }

    [Fact]
    public void Decode_RejectsInvalidMagic()
    {
        using var bmp = CreateTestBitmap(8, 8);
        byte[] encoded = MsiEncoder.Encode(bmp);
        encoded[0] = (byte)'X';

        Assert.Throws<InvalidDataException>(() => MsiDecoder.Decode(encoded));
    }

    [Fact]
    public void Crc32_MatchesKnownVector()
    {
        var data = System.Text.Encoding.ASCII.GetBytes("123456789");
        Assert.Equal(0xCBF43926u, Crc32.Compute(data));
    }

    [Fact]
    public void Huffman_RoundTrip()
    {
        var data = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
        var (compressed, meta) = HuffmanCodec.Compress(data);
        var restored = HuffmanCodec.Decompress(compressed, meta, data.Length);
        Assert.Equal(data, restored);
    }

    private static Bitmap CreateTestBitmap(int w, int h)
    {
        var bmp = new Bitmap(w, h, PixelFormat.Format24bppRgb);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.Coral);
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                bmp.SetPixel(x, y, Color.FromArgb((x * 7 + y * 13) % 256, (x * 3) % 256, (y * 5) % 256));
        return bmp;
    }
}
