using System.Drawing;
using System.Drawing.Imaging;

namespace Server.Core;

public static class ImageConverter
{
    private static readonly byte[] PngMagic = [0x89, 0x50, 0x4E, 0x47];
    private static readonly byte[] JpegMagic = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] BmpMagic = [0x42, 0x4D];
    private static readonly byte[] GifMagic = [0x47, 0x49, 0x46];
    private static readonly byte[] MsiMagic = [0x4D, 0x53, 0x49, 0x30];

    public const int MaxUploadBytes = 20 * 1024 * 1024;

    public static void ValidateImageBytes(byte[] data, string? fileName = null)
    {
        if (data == null || data.Length == 0)
            throw new ArgumentException("Image data is empty");

        if (data.Length > MaxUploadBytes)
            throw new ArgumentException($"Image exceeds {MaxUploadBytes / (1024 * 1024)} MB limit");

        if (fileName != null)
        {
            var name = Path.GetFileName(fileName);
            if (string.IsNullOrWhiteSpace(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentException("Invalid file name");

            var ext = Path.GetExtension(name).ToLowerInvariant();
            if (ext is not (".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".msi"))
                throw new ArgumentException($"Unsupported file extension: {ext}");
        }

        if (!HasMagic(data, PngMagic) && !HasMagic(data, JpegMagic) &&
            !HasMagic(data, BmpMagic) && !HasMagic(data, GifMagic) &&
            !HasMagic(data, MsiMagic))
            throw new ArgumentException("Unrecognized image format (invalid magic bytes)");
    }

    public static Bitmap FromBytes(byte[] data)
    {
        ValidateImageBytes(data);

        if (HasMagic(data, MsiMagic))
            return Shared.MSI.MsiDecoder.Decode(data);

        using var ms = new MemoryStream(data);
        return new Bitmap(ms);
    }

    public static byte[] ToBytes(Bitmap bitmap)
    {
        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }

    private static bool HasMagic(byte[] data, byte[] magic)
    {
        if (data.Length < magic.Length) return false;
        for (int i = 0; i < magic.Length; i++)
            if (data[i] != magic[i]) return false;
        return true;
    }
}
