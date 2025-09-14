using DiscountAggregator.Domain.Entities;
using DiscountAggregator.Domain.Interfaces;
using DiscountAggregator.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiscountAggregator.Infrastructure.Repositories
{
    public class DbApiSubscriptionRepository : IApiSubscriptionRepository
    {
        private readonly AppDbContext _db;
        public DbApiSubscriptionRepository(AppDbContext db) { _db = db; }

        public async Task<ApiSubscription> GetOrCreateAsync(string sourceKey, string keyword, CancellationToken ct = default)
        {
            var existing = await _db.ApiSubscriptions.FirstOrDefaultAsync(a => a.SourceKey == sourceKey && a.Keyword == keyword, ct);
            if (existing != null) return existing;
            var created = new ApiSubscription { Id = Guid.NewGuid(), SourceKey = sourceKey, Keyword = keyword, CreatedAtUtc = DateTime.UtcNow };
            _db.ApiSubscriptions.Add(created);
            await _db.SaveChangesAsync(ct);
            return created;
        }

        public async Task<ApiSubscription?> GetAsync(string sourceKey, string keyword, CancellationToken ct = default)
        {
            return await _db.ApiSubscriptions.FirstOrDefaultAsync(a => a.SourceKey == sourceKey && a.Keyword == keyword, ct);
        }

        public async Task<IReadOnlyList<ApiSubscription>> GetAllAsync(CancellationToken ct = default)
        {
            return await _db.ApiSubscriptions.AsNoTracking().ToListAsync(ct);
        }
    }
}

