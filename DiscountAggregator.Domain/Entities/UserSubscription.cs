namespace DiscountAggregator.Domain.Entities
{
    public class UserSubscription
    {
        public Guid Id { get; set; }
        public long UserId { get; set; }
        public Guid ApiSubscriptionId { get; set; }
        public bool Subscribed { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }
}

