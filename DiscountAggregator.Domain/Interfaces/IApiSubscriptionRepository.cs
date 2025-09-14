using DiscountAggregator.Domain.Entities;

namespace DiscountAggregator.Domain.Interfaces
{
    public interface IApiSubscriptionRepository
    {
        Task<ApiSubscription> GetOrCreateAsync(string sourceKey, string keyword, CancellationToken ct = default);
        Task<ApiSubscription?> GetAsync(string sourceKey, string keyword, CancellationToken ct = default);
        Task<IReadOnlyList<ApiSubscription>> GetAllAsync(CancellationToken ct = default);
    }
}

