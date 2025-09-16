using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DiscountAggregator.Domain.Entities;

namespace DiscountAggregator.Application.Interfaces
{
    public interface IProductCacheService
    {
        Task<IEnumerable<Product>> GetCachedProductsAsync(string keyword, CancellationToken ct = default);
        Task SetCachedProductsAsync(string keyword, IEnumerable<Product> products, TimeSpan expiration, CancellationToken ct = default);
        Task<bool> IsCachedAsync(string keyword, CancellationToken ct = default);
        Task ClearCacheAsync(string keyword, CancellationToken ct = default);
    }
}