using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DiscountAggregator.Domain.Entities;

namespace DiscountAggregator.Domain.Interfaces
{
    public interface IProductRepository
    {
        Task UpsertAsync(Product product, CancellationToken ct = default);
        Task<IEnumerable<Product>> SearchAsync(string keyword, CancellationToken ct = default);
        Task<IEnumerable<Product>> GetRecentAsync(int hours, CancellationToken ct = default);
        Task<IEnumerable<Product>> SearchSinceAsync(string keyword, DateTime sinceUtc, CancellationToken ct = default);
        Task<Product?> GetBySourceAndExternalIdAsync(string source, string externalId, CancellationToken ct = default);
    }
}
