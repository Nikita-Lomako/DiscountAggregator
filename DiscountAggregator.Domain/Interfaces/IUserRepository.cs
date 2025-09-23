using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DiscountAggregator.Domain.Entities;

namespace DiscountAggregator.Domain.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(long userId, CancellationToken ct = default);
        Task UpsertAsync(User user, CancellationToken ct = default);
        Task<IEnumerable<User>> GetActiveUsersAsync(int hours, CancellationToken ct = default);
    }
}