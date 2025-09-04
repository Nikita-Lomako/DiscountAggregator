using DiscountAggregator.Domain.Entities;

namespace DiscountAggregator.Domain.Interfaces
{
    public interface IQueryLogRepository
    {
        Task AddAsync(QueryLog log, CancellationToken ct = default);
        Task<IReadOnlyList<QueryLog>> GetRecentAsync(long userId, TimeSpan window, CancellationToken ct = default);
    }
}

