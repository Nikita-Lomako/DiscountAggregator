namespace DiscountAggregator.Domain.Entities
{
    public class ApiSubscription
    {
        public Guid Id { get; set; }
        public string SourceKey { get; set; } = string.Empty; // wildberries, ozon, etc.
        public string Keyword { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
    }
}

