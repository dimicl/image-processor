using Shared.Enums;

public class MsiHeader
{
    public string Magic { get; set; } = "MSI0";

    public ushort Version { get; set; } = 1;

    public ushort HeaderLength { get; set; }

    public uint Width { get; set; }

    public uint Height { get; set; }

    public byte Channels { get; set; }

    public ColorSpace ColorSpace { get; set; }

    public CompressionType Compression { get; set; }

    public byte Reserved { get; set; }

    public int MetaLength { get; set; }

    public int PixelLength { get; set; }
}