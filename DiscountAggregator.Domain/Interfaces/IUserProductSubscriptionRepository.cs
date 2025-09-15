using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DiscountAggregator.Domain.Entities;

namespace DiscountAggregator.Domain.Interfaces
{
    public interface IUserProductSubscriptionRepository
    {
        Task<IEnumerable<UserProductSubscription>> GetByUserIdAsync(long userId, CancellationToken ct = default);
        Task<IEnumerable<UserProductSubscription>> GetActiveByUserIdAsync(long userId, CancellationToken ct = default);
        Task<UserProductSubscription?> GetByUserAndProductAsync(long userId, Guid productId, CancellationToken ct = default);
        Task UpsertAsync(UserProductSubscription subscription, CancellationToken ct = default);
        Task DeleteAsync(long userId, Guid productId, CancellationToken ct = default);
        Task<IEnumerable<Product>> GetSubscribedProductsAsync(long userId, CancellationToken ct = default);
    }
}