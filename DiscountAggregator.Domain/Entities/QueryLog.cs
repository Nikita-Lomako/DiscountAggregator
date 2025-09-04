namespace DiscountAggregator.Domain.Entities
{
    public class QueryLog
    {
        public Guid Id { get; set; }
        public long UserId { get; set; }
        public string Keyword { get; set; } = string.Empty;
        public DateTime QueriedAtUtc { get; set; }
    }
}

