namespace DiscountAggregator.Domain.Entities
{
    public class User
    {
        public long Id { get; set; } // Telegram user id
        public string? Username { get; set; }
        public DateTime RegisteredAtUtc { get; set; }
        public DateTime LastActivityAtUtc { get; set; }

        public ICollection<SearchQuery> SearchHistory { get; set; } = new List<SearchQuery>();
    }
}

