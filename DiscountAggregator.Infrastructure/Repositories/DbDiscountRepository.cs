using DiscountAggregator.Domain.Entities;
using DiscountAggregator.Domain.Interfaces;
using DiscountAggregator.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiscountAggregator.Infrastructure.Repositories
{
    public class DbDiscountRepository : IDiscountRepository
    {
        private readonly AppDbContext _db;
        public DbDiscountRepository(AppDbContext db) { _db = db; }

        public async Task UpsertAsync(Discount discount, CancellationToken ct = default)
        {
            var exists = await _db.Discounts.AsNoTracking().FirstOrDefaultAsync(d => d.Fingerprint == discount.Fingerprint, ct);
            if (exists is null)
            {
                await _db.Discounts.AddAsync(discount, ct);
            }
            else
            {
                discount.Id = exists.Id;
                _db.Discounts.Update(discount);
            }
            await _db.SaveChangesAsync(ct);
        }

        public async Task<IEnumerable<Discount>> SearchAsync(string keyword, CancellationToken ct = default)
        {
            return await _db.Discounts
                .Where(d => EF.Functions.ILike(d.Title, $"%{keyword}%") || EF.Functions.ILike(d.Brand, $"%{keyword}%"))
                .OrderByDescending(d => d.FetchedAtUtc)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<Discount>> GetRecentAsync(int hours, CancellationToken ct = default)
        {
            var threshold = DateTime.UtcNow.AddHours(-hours);
            return await _db.Discounts
                .Where(d => d.FetchedAtUtc >= threshold)
                .OrderByDescending(d => d.FetchedAtUtc)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<Discount>> SearchSinceAsync(string keyword, DateTime sinceUtc, CancellationToken ct = default)
        {
            return await _db.Discounts
                .Where(d => d.FetchedAtUtc >= sinceUtc)
                .Where(d => EF.Functions.ILike(d.Title, $"%{keyword}%") || EF.Functions.ILike(d.Brand, $"%{keyword}%"))
                .OrderByDescending(d => d.FetchedAtUtc)
                .ToListAsync(ct);
        }
    }
}

