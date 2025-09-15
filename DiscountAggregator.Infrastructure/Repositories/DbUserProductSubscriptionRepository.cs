using DiscountAggregator.Domain.Entities;
using DiscountAggregator.Domain.Interfaces;
using DiscountAggregator.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiscountAggregator.Infrastructure.Repositories
{
    public class DbUserProductSubscriptionRepository : IUserProductSubscriptionRepository
    {
        private readonly AppDbContext _db;
        public DbUserProductSubscriptionRepository(AppDbContext db) { _db = db; }

        public async Task<IEnumerable<UserProductSubscription>> GetByUserIdAsync(long userId, CancellationToken ct = default)
        {
            return await _db.UserProductSubscriptions
                .Include(s => s.Product)
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.SubscribedAtUtc)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<UserProductSubscription>> GetActiveByUserIdAsync(long userId, CancellationToken ct = default)
        {
            return await _db.UserProductSubscriptions
                .Include(s => s.Product)
                .Where(s => s.UserId == userId && s.IsActive)
                .OrderByDescending(s => s.SubscribedAtUtc)
                .ToListAsync(ct);
        }

        public async Task<UserProductSubscription?> GetByUserAndProductAsync(long userId, Guid productId, CancellationToken ct = default)
        {
            return await _db.UserProductSubscriptions
                .Include(s => s.Product)
                .FirstOrDefaultAsync(s => s.UserId == userId && s.ProductId == productId, ct);
        }

        public async Task UpsertAsync(UserProductSubscription subscription, CancellationToken ct = default)
        {
            var exists = await _db.UserProductSubscriptions
                .FirstOrDefaultAsync(s => s.UserId == subscription.UserId && s.ProductId == subscription.ProductId, ct);
            
            if (exists is null)
            {
                subscription.SubscribedAtUtc = DateTime.UtcNow;
                await _db.UserProductSubscriptions.AddAsync(subscription, ct);
            }
            else
            {
                exists.IsActive = subscription.IsActive;
                exists.SubscribedAtUtc = subscription.SubscribedAtUtc;
                _db.UserProductSubscriptions.Update(exists);
            }
            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(long userId, Guid productId, CancellationToken ct = default)
        {
            var subscription = await _db.UserProductSubscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.ProductId == productId, ct);
            
            if (subscription != null)
            {
                _db.UserProductSubscriptions.Remove(subscription);
                await _db.SaveChangesAsync(ct);
            }
        }

        public async Task<IEnumerable<Product>> GetSubscribedProductsAsync(long userId, CancellationToken ct = default)
        {
            return await _db.UserProductSubscriptions
                .Where(s => s.UserId == userId && s.IsActive)
                .Select(s => s.Product)
                .OrderByDescending(p => p.LastUpdatedAtUtc)
                .ToListAsync(ct);
        }
    }
}