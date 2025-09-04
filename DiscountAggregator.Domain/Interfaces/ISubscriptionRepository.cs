using DiscountAggregator.Domain.Entities;

namespace DiscountAggregator.Domain.Interfaces
{
    public interface ISubscriptionRepository
    {
        Task AddAsync(Subscription subscription, CancellationToken ct = default);
        Task RemoveAsync(long userId, string keyword, CancellationToken ct = default);
        Task<bool> ExistsAsync(long userId, string keyword, CancellationToken ct = default);
        Task<IReadOnlyList<Subscription>> GetByUserAsync(long userId, CancellationToken ct = default);
    }
}

