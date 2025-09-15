using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DiscountAggregator.Domain.Entities;

namespace DiscountAggregator.Domain.Interfaces
{
    public interface IProductPriceHistoryRepository
    {
        Task AddPriceRecordAsync(Guid productId, decimal price, DateTime recordedAtUtc, CancellationToken ct = default);
        Task<IEnumerable<ProductPriceHistory>> GetByProductIdAsync(Guid productId, CancellationToken ct = default);
        Task<ProductPriceHistory?> GetLatestByProductIdAsync(Guid productId, CancellationToken ct = default);
        Task<IEnumerable<ProductPriceHistory>> GetByProductIdSinceAsync(Guid productId, DateTime sinceUtc, CancellationToken ct = default);
    }
}