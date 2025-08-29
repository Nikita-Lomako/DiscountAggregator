namespace DiscountAggregator.Application.DTOs
{
    public class SourceFetchRequest
    {
        public int Limit { get; set; }
        public string Keyword { get; set; } = string.Empty;
    }
} 