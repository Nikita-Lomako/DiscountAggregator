namespace DiscountAggregator.Application.DTOs
{
    public class DiscountDto
    {
        public string Source { get; set; } = string.Empty;
        public string ExternalId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal OldPrice { get; set; }
        public string Url { get; set; } = string.Empty;
        public DateTime FetchedAtUtc { get; set; }
        public string Fingerprint { get; set; } = string.Empty;
    }
}
