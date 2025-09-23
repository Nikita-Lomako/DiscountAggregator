using DiscountAggregator.Domain.Entities;
using DiscountAggregator.Domain.Interfaces;
using DiscountAggregator.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiscountAggregator.Infrastructure.Repositories
{
    public class DbSearchQueryRepository : ISearchQueryRepository
    {
        private readonly AppDbContext _db;
        public DbSearchQueryRepository(AppDbContext db) { _db = db; }

        public async Task AddQueryAsync(SearchQuery query, CancellationToken ct = default)
        {
            query.Id = Guid.NewGuid();
            query.QueriedAtUtc = DateTime.UtcNow;
            await _db.SearchQueries.AddAsync(query, ct);
            await _db.SaveChangesAsync(ct);
        }

        public async Task<IEnumerable<SearchQuery>> GetByUserIdAsync(long userId, CancellationToken ct = default)
        {
            return await _db.SearchQueries
                .Where(q => q.UserId == userId)
                .OrderByDescending(q => q.QueriedAtUtc)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<SearchQuery>> GetRecentByUserIdAsync(long userId, int hours, CancellationToken ct = default)
        {
            var threshold = DateTime.UtcNow.AddHours(-hours);
            return await _db.SearchQueries
                .Where(q => q.UserId == userId && q.QueriedAtUtc >= threshold)
                .OrderByDescending(q => q.QueriedAtUtc)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<SearchQuery>> GetRecentQueriesAsync(int hours, CancellationToken ct = default)
        {
            var threshold = DateTime.UtcNow.AddHours(-hours);
            return await _db.SearchQueries
                .Where(q => q.QueriedAtUtc >= threshold)
                .OrderByDescending(q => q.QueriedAtUtc)
                .ToListAsync(ct);
        }
    }
}