namespace DiscountAggregator.Domain.Entities
{
    public class UserCategorySubscription
    {
        public long UserId { get; set; }
        public string Keyword { get; set; } = string.Empty;
        public string SourceFilter { get; set; } = string.Empty;
        public DateTime SubscribedAtUtc { get; set; }
        public bool IsActive { get; set; } = true;

        public User User { get; set; } = null!;
    }
}