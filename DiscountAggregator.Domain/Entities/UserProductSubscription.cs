namespace DiscountAggregator.Domain.Entities
{
    public class UserProductSubscription
    {
        public long UserId { get; set; }
        public Guid ProductId { get; set; }
        public DateTime SubscribedAtUtc { get; set; }
        public bool IsActive { get; set; } = true;

        public User User { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}

