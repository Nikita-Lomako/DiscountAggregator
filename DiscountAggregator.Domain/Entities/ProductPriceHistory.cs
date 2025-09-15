namespace DiscountAggregator.Domain.Entities
{
    public class ProductPriceHistory
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public decimal Price { get; set; }
        public DateTime RecordedAtUtc { get; set; }

        public Product Product { get; set; } = null!;
    }
}

