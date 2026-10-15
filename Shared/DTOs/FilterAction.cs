namespace Shared.DTOs
{
    public class FilterAction
    {
        public FilterType Filter { get; set; }

        public int Intensity { get; set; } = 0;

        public Dictionary<string, object>? Parameters { get; set; }
    }
}