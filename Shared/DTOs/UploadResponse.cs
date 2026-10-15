namespace Shared.DTOs;

public class UploadImageResponse
{
    public Guid ImageId { get; set; }
    public string Message { get; set; } = "";
    public int Width { get; set; }
    public int Height { get; set; }
}
