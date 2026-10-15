public class MsiFile
{
    public MsiHeader? Header { get; set; }

    public byte[]? Metadata { get; set; }

    public byte[]? PixelData { get; set; }

    public uint Crc32 { get; set; }
}