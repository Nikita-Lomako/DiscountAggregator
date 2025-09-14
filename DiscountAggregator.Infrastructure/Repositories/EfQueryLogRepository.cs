using DiscountAggregator.Domain.Entities;
using DiscountAggregator.Domain.Interfaces;
using DiscountAggregator.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiscountAggregator.Infrastructure.Repositories
{
    public class EfQueryLogRepository : IQueryLogRepository
    {
        private readonly AppDbContext _dbContext;

        public EfQueryLogRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(QueryLog log, CancellationToken ct = default)
        {
            await _dbContext.QueryLogs.AddAsync(log, ct);
            await _dbContext.SaveChangesAsync(ct);
        }

        public async Task<IReadOnlyList<QueryLog>> GetRecentAsync(long userId, TimeSpan window, CancellationToken ct = default)
        {
            var threshold = DateTime.UtcNow - window;
            var list = await _dbContext.QueryLogs
                .Where(q => q.UserId == userId && q.QueriedAtUtc >= threshold)
                .OrderByDescending(q => q.QueriedAtUtc)
                .ToListAsync(ct);
            return list;
        }
    }
}

