using DiscountAggregator.Domain.Entities;
using DiscountAggregator.Domain.Interfaces;
using DiscountAggregator.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DiscountAggregator.Infrastructure.Repositories
{
    public class DbUserRepository : IUserRepository
    {
        private readonly AppDbContext _db;
        public DbUserRepository(AppDbContext db) { _db = db; }

        public async Task<User?> GetByIdAsync(long userId, CancellationToken ct = default)
        {
            return await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        }

        public async Task UpsertAsync(User user, CancellationToken ct = default)
        {
            var exists = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == user.Id, ct);
            
            if (exists is null)
            {
                user.RegisteredAtUtc = DateTime.UtcNow;
                user.LastActivityAtUtc = DateTime.UtcNow;
                await _db.Users.AddAsync(user, ct);
            }
            else
            {
                user.RegisteredAtUtc = exists.RegisteredAtUtc;
                user.LastActivityAtUtc = DateTime.UtcNow;
                _db.Users.Update(user);
            }
            await _db.SaveChangesAsync(ct);
        }

        public async Task<IEnumerable<User>> GetActiveUsersAsync(int hours, CancellationToken ct = default)
        {
            var threshold = DateTime.UtcNow.AddHours(-hours);
            return await _db.Users
                .Where(u => u.LastActivityAtUtc >= threshold)
                .OrderByDescending(u => u.LastActivityAtUtc)
                .ToListAsync(ct);
        }
    }
}