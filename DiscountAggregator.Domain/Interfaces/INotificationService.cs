using System.Threading;
using System.Threading.Tasks;

namespace DiscountAggregator.Domain.Interfaces
{
    public interface INotificationService
    {
        Task NotifyAsync(long userId, string message, CancellationToken ct = default);
    }
}
