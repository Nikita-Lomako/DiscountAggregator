using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DiscountAggregator.Domain.Entities;

namespace DiscountAggregator.Domain.Interfaces
{
    public interface ISearchQueryRepository
    {
        Task AddQueryAsync(SearchQuery query, CancellationToken ct = default);
        Task<IEnumerable<SearchQuery>> GetByUserIdAsync(long userId, CancellationToken ct = default);
        Task<IEnumerable<SearchQuery>> GetRecentByUserIdAsync(long userId, int hours, CancellationToken ct = default);
        Task<IEnumerable<SearchQuery>> GetRecentQueriesAsync(int hours, CancellationToken ct = default);
    }
}