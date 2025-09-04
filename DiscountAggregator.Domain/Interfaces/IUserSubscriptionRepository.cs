using DiscountAggregator.Domain.Entities;

namespace DiscountAggregator.Domain.Interfaces
{
    public interface IUserSubscriptionRepository
    {
        Task UpsertAsync(UserSubscription subscription, CancellationToken ct = default);
        Task<UserSubscription?> GetAsync(long userId, Guid apiSubscriptionId, CancellationToken ct = default);
        Task<IReadOnlyList<UserSubscription>> GetByUserAsync(long userId, CancellationToken ct = default);
        Task<IReadOnlyList<UserSubscription>> GetSubscribedUsersAsync(Guid apiSubscriptionId, CancellationToken ct = default);
    }
}

