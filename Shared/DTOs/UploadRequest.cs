namespace Shared.DTOs;

public class UploadImageRequest
{
    public byte[] Image { get; set; } = Array.Empty<byte>();
    public string? FileName { get; set; }
}
