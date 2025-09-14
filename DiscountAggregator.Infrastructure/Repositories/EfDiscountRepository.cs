using DiscountAggregator.Domain.Entities;
using DiscountAggregator.Domain.Interfaces;
using DiscountAggregator.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiscountAggregator.Infrastructure.Repositories
{
    public class EfDiscountRepository : IDiscountRepository
    {
        private readonly AppDbContext _dbContext;

        public EfDiscountRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task UpsertAsync(Discount discount, CancellationToken ct = default)
        {
            var existing = await _dbContext.Discounts
                .FirstOrDefaultAsync(d => d.Fingerprint == discount.Fingerprint, ct);
            if (existing is null)
            {
                await _dbContext.Discounts.AddAsync(discount, ct);
            }
            else
            {
                _dbContext.Entry(existing).CurrentValues.SetValues(discount);
            }
            await _dbContext.SaveChangesAsync(ct);
        }

        public async Task<IEnumerable<Discount>> SearchAsync(string keyword, CancellationToken ct = default)
        {
            return await _dbContext.Discounts
                .Where(d => EF.Functions.ILike(d.Title, $"%{keyword}%") || EF.Functions.ILike(d.Brand, $"%{keyword}%"))
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<Discount>> GetRecentAsync(int hours, CancellationToken ct = default)
        {
            var threshold = DateTime.UtcNow.AddHours(-hours);
            return await _dbContext.Discounts
                .Where(d => d.FetchedAtUtc >= threshold)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<Discount>> SearchSinceAsync(string keyword, DateTime sinceUtc, CancellationToken ct = default)
        {
            return await _dbContext.Discounts
                .Where(d => d.FetchedAtUtc >= sinceUtc)
                .Where(d => EF.Functions.ILike(d.Title, $"%{keyword}%") || EF.Functions.ILike(d.Brand, $"%{keyword}%"))
                .ToListAsync(ct);
        }
    }
}

