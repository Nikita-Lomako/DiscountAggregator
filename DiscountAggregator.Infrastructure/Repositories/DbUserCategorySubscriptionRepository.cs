using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DiscountAggregator.Domain.Entities;
using DiscountAggregator.Domain.Interfaces;
using DiscountAggregator.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiscountAggregator.Infrastructure.Repositories
{
    public class DbUserCategorySubscriptionRepository : IUserCategorySubscriptionRepository
    {
        private readonly AppDbContext _context;

        public DbUserCategorySubscriptionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<UserCategorySubscription?> GetByUserAndKeywordAsync(long userId, string keyword, string sourceFilter, CancellationToken ct = default)
        {
            return await _context.UserCategorySubscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Keyword == keyword && s.SourceFilter == sourceFilter, ct);
        }

        public async Task<IEnumerable<UserCategorySubscription>> GetActiveByUserIdAsync(long userId, CancellationToken ct = default)
        {
            return await _context.UserCategorySubscriptions
                .Where(s => s.UserId == userId && s.IsActive)
                .OrderByDescending(s => s.SubscribedAtUtc)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<UserCategorySubscription>> GetAllActiveSubscriptionsAsync(CancellationToken ct = default)
        {
            return await _context.UserCategorySubscriptions
                .Where(s => s.IsActive)
                .Include(s => s.User)
                .ToListAsync(ct);
        }

        public async Task UpsertAsync(UserCategorySubscription subscription, CancellationToken ct = default)
        {
            var existing = await GetByUserAndKeywordAsync(subscription.UserId, subscription.Keyword, subscription.SourceFilter, ct);
            
            if (existing == null)
            {
                _context.UserCategorySubscriptions.Add(subscription);
            }
            else
            {
                existing.IsActive = subscription.IsActive;
                existing.SubscribedAtUtc = subscription.SubscribedAtUtc;
            }
            
            await _context.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(long userId, string keyword, string sourceFilter, CancellationToken ct = default)
        {
            var subscription = await GetByUserAndKeywordAsync(userId, keyword, sourceFilter, ct);
            if (subscription != null)
            {
                subscription.IsActive = false;
                await _context.SaveChangesAsync(ct);
            }
        }
    }
}