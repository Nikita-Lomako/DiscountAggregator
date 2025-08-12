namespace DiscountAggregator.Domain.Entities
{
    public class Subscription
    {
        public Guid Id { get; set; }
        public long UserId { get; set; } // Telegram user id
        public string Keyword { get; set; } = string.Empty;
        public DateTime SubscribedAtUtc { get; set; }
    }
}
