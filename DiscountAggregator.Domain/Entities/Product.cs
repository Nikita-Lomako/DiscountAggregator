namespace DiscountAggregator.Domain.Entities
{
    public class Product
    {
        public Guid Id { get; set; }
        public string Source { get; set; } = string.Empty; // wildberries/ozon
        public string ExternalId { get; set; } = string.Empty; // id on source
        public string Title { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public decimal CurrentPrice { get; set; }
        public decimal OldPrice { get; set; }
        public string Url { get; set; } = string.Empty;
        public DateTime LastUpdatedAtUtc { get; set; }

        // Navigation
        public ICollection<ProductPriceHistory> PriceHistory { get; set; } = new List<ProductPriceHistory>();
        public ICollection<UserProductSubscription> UserSubscriptions { get; set; } = new List<UserProductSubscription>();

        public decimal DiscountPercent => OldPrice > 0 ? Math.Round(100 * (OldPrice - CurrentPrice) / OldPrice, 2) : 0;
    }
}

