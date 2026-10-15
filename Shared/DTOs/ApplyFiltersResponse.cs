namespace Shared.DTOs;

public class ApplyFiltersResponse
{
    public byte[] Image { get; set; } = Array.Empty<byte>();
    public long TotalProcessingTimeMs { get; set; }
    public List<FilterTimingDto> FilterTimings { get; set; } = new();
}

public class FilterTimingDto
{
    public string Filter { get; set; } = "";
    public long DurationMs { get; set; }
}
