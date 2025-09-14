namespace DiscountAggregator.Application.DTOs
{
    public class RawDiscountDto
    {
        public string ExternalId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal OldPrice { get; set; }
        public string Url { get; set; } = string.Empty;
    }
} 