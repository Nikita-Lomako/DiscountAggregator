using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DiscountAggregator.Domain.Entities;

namespace DiscountAggregator.Domain.Interfaces
{
    public interface IDiscountRepository
    {
        Task UpsertAsync(Discount discount, CancellationToken ct = default);
        Task<IEnumerable<Discount>> SearchAsync(string keyword, CancellationToken ct = default);
        Task<IEnumerable<Discount>> GetRecentAsync(int hours, CancellationToken ct = default);
    }
}
