using Shared.Enums;

namespace Shared.Meta;

public class FilterMetadata
{
    public List<FilterType> AppliedFilters { get; set; } = new();

    public Dictionary<string, object> Parameters { get; set; } = new();
}