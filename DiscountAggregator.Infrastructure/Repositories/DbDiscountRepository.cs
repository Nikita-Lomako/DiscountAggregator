using DiscountAggregator.Domain.Entities;
using DiscountAggregator.Domain.Interfaces;
using DiscountAggregator.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiscountAggregator.Infrastructure.Repositories
{
    public class DbProductRepository : IProductRepository
    {
        private readonly AppDbContext _db;
        public DbProductRepository(AppDbContext db) { _db = db; }

        public async Task UpsertAsync(Product product, CancellationToken ct = default)
        {
            var exists = await _db.Products.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Source == product.Source && p.ExternalId == product.ExternalId, ct);
            
            if (exists is null)
            {
                product.Id = Guid.NewGuid();
                product.LastUpdatedAtUtc = DateTime.UtcNow;
                await _db.Products.AddAsync(product, ct);
            }
            else
            {
                product.Id = exists.Id;
                product.LastUpdatedAtUtc = DateTime.UtcNow;
                _db.Products.Update(product);
            }
            await _db.SaveChangesAsync(ct);
        }

        public async Task<IEnumerable<Product>> SearchAsync(string keyword, CancellationToken ct = default)
        {
            return await _db.Products
                .Where(p => EF.Functions.ILike(p.Title, $"%{keyword}%") || EF.Functions.ILike(p.Brand, $"%{keyword}%"))
                .OrderByDescending(p => p.LastUpdatedAtUtc)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<Product>> GetRecentAsync(int hours, CancellationToken ct = default)
        {
            var threshold = DateTime.UtcNow.AddHours(-hours);
            return await _db.Products
                .Where(p => p.LastUpdatedAtUtc >= threshold)
                .OrderByDescending(p => p.LastUpdatedAtUtc)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<Product>> SearchSinceAsync(string keyword, DateTime sinceUtc, CancellationToken ct = default)
        {
            return await _db.Products
                .Where(p => p.LastUpdatedAtUtc >= sinceUtc)
                .Where(p => EF.Functions.ILike(p.Title, $"%{keyword}%") || EF.Functions.ILike(p.Brand, $"%{keyword}%"))
                .OrderByDescending(p => p.LastUpdatedAtUtc)
                .ToListAsync(ct);
        }

        public async Task<Product?> GetBySourceAndExternalIdAsync(string source, string externalId, CancellationToken ct = default)
        {
            return await _db.Products
                .FirstOrDefaultAsync(p => p.Source == source && p.ExternalId == externalId, ct);
        }

        public async Task<int> DeleteByKeywordAsync(string keyword, CancellationToken ct = default)
        {
            var productsToDelete = await _db.Products
                .Where(p => EF.Functions.ILike(p.Title, $"%{keyword}%") || EF.Functions.ILike(p.Brand, $"%{keyword}%"))
                .ToListAsync(ct);

            if (productsToDelete.Any())
            {
                _db.Products.RemoveRange(productsToDelete);
                await _db.SaveChangesAsync(ct);
            }

            return productsToDelete.Count;
        }
    }
}

