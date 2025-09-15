using DiscountAggregator.Domain.Entities;
using DiscountAggregator.Domain.Interfaces;
using DiscountAggregator.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiscountAggregator.Infrastructure.Repositories
{
    public class DbProductPriceHistoryRepository : IProductPriceHistoryRepository
    {
        private readonly AppDbContext _db;
        public DbProductPriceHistoryRepository(AppDbContext db) { _db = db; }

        public async Task AddPriceRecordAsync(Guid productId, decimal price, DateTime recordedAtUtc, CancellationToken ct = default)
        {
            var historyRecord = new ProductPriceHistory
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                Price = price,
                RecordedAtUtc = recordedAtUtc
            };

            await _db.ProductPriceHistories.AddAsync(historyRecord, ct);
            await _db.SaveChangesAsync(ct);
        }

        public async Task<IEnumerable<ProductPriceHistory>> GetByProductIdAsync(Guid productId, CancellationToken ct = default)
        {
            return await _db.ProductPriceHistories
                .Where(h => h.ProductId == productId)
                .OrderByDescending(h => h.RecordedAtUtc)
                .ToListAsync(ct);
        }

        public async Task<ProductPriceHistory?> GetLatestByProductIdAsync(Guid productId, CancellationToken ct = default)
        {
            return await _db.ProductPriceHistories
                .Where(h => h.ProductId == productId)
                .OrderByDescending(h => h.RecordedAtUtc)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<IEnumerable<ProductPriceHistory>> GetByProductIdSinceAsync(Guid productId, DateTime sinceUtc, CancellationToken ct = default)
        {
            return await _db.ProductPriceHistories
                .Where(h => h.ProductId == productId && h.RecordedAtUtc >= sinceUtc)
                .OrderByDescending(h => h.RecordedAtUtc)
                .ToListAsync(ct);
        }
    }
}