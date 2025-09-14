using DiscountAggregator.Domain.Entities;
using DiscountAggregator.Domain.Interfaces;
using DiscountAggregator.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiscountAggregator.Infrastructure.Repositories
{
    public class DbUserSubscriptionRepository : IUserSubscriptionRepository
    {
        private readonly AppDbContext _db;
        public DbUserSubscriptionRepository(AppDbContext db) { _db = db; }

        public async Task UpsertAsync(UserSubscription subscription, CancellationToken ct = default)
        {
            var existing = await _db.UserSubscriptions.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == subscription.UserId && x.ApiSubscriptionId == subscription.ApiSubscriptionId, ct);
            if (existing is null)
            {
                await _db.UserSubscriptions.AddAsync(subscription, ct);
            }
            else
            {
                subscription.Id = existing.Id;
                _db.UserSubscriptions.Update(subscription);
            }
            await _db.SaveChangesAsync(ct);
        }

        public async Task<UserSubscription?> GetAsync(long userId, Guid apiSubscriptionId, CancellationToken ct = default)
        {
            return await _db.UserSubscriptions.FirstOrDefaultAsync(x => x.UserId == userId && x.ApiSubscriptionId == apiSubscriptionId, ct);
        }

        public async Task<IReadOnlyList<UserSubscription>> GetByUserAsync(long userId, CancellationToken ct = default)
        {
            return await _db.UserSubscriptions.Where(x => x.UserId == userId).ToListAsync(ct);
        }

        public async Task<IReadOnlyList<UserSubscription>> GetSubscribedUsersAsync(Guid apiSubscriptionId, CancellationToken ct = default)
        {
            return await _db.UserSubscriptions.Where(x => x.ApiSubscriptionId == apiSubscriptionId && x.Subscribed).ToListAsync(ct);
        }
    }
}

