namespace Shared.DTOs;

public class ApplyFiltersRequest
{
    public byte[]? Image { get; set; }
    public Guid? ImageId { get; set; }
    public List<FilterAction> Filters { get; set; } = new();
}
