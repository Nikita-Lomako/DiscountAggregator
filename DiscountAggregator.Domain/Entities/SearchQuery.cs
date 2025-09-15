namespace DiscountAggregator.Domain.Entities
{
    public class SearchQuery
    {
        public Guid Id { get; set; }
        public long UserId { get; set; }
        public string Keyword { get; set; } = string.Empty;
        public string? SourceFilter { get; set; }
        public string? KeywordNormalized { get; set; }
        public DateTime QueriedAtUtc { get; set; }

        public User User { get; set; } = null!;
    }
}

