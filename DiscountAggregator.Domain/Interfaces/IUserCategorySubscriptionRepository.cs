using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DiscountAggregator.Domain.Entities;

namespace DiscountAggregator.Domain.Interfaces
{
    public interface IUserCategorySubscriptionRepository
    {
        Task<UserCategorySubscription?> GetByUserAndKeywordAsync(long userId, string keyword, string sourceFilter, CancellationToken ct = default);
        Task<IEnumerable<UserCategorySubscription>> GetActiveByUserIdAsync(long userId, CancellationToken ct = default);
        Task<IEnumerable<UserCategorySubscription>> GetAllActiveSubscriptionsAsync(CancellationToken ct = default);
        Task UpsertAsync(UserCategorySubscription subscription, CancellationToken ct = default);
        Task DeleteAsync(long userId, string keyword, string sourceFilter, CancellationToken ct = default);
    }
}