using DiscountAggregator.Domain.Entities;
using DiscountAggregator.Domain.Interfaces;
using DiscountAggregator.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiscountAggregator.Infrastructure.Repositories
{
    public class DbQueryLogRepository : IQueryLogRepository
    {
        private readonly AppDbContext _db;
        public DbQueryLogRepository(AppDbContext db) { _db = db; }

        public async Task AddAsync(QueryLog log, CancellationToken ct = default)
        {
            await _db.QueryLogs.AddAsync(log, ct);
            await _db.SaveChangesAsync(ct);
        }

        public async Task<IReadOnlyList<QueryLog>> GetRecentAsync(long userId, TimeSpan window, CancellationToken ct = default)
        {
            var since = DateTime.UtcNow - window;
            return await _db.QueryLogs
                .Where(x => x.UserId == userId && x.QueriedAtUtc >= since)
                .OrderByDescending(x => x.QueriedAtUtc)
                .ToListAsync(ct);
        }
    }
}

