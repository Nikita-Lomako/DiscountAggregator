namespace DiscountAggregator.Domain.Entities
{
    public class Discount
    {
        public Guid Id { get; set; }
        public string Source { get; set; } = string.Empty;         // wildberries
        public string ExternalId { get; set; } = string.Empty;       // id на площадке
        public string Title { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal OldPrice { get; set; }
        public decimal DiscountPercent => OldPrice > 0 ? Math.Round(100 * (OldPrice - Price) / OldPrice, 2) : 0;
        public string Url { get; set; } = string.Empty;
        public DateTime FetchedAtUtc { get; set; }
        public string Fingerprint { get; set; } = string.Empty; // hash(Source + ExternalId)
    }
}
