using System.Threading;
using System.Threading.Tasks;
using DiscountAggregator.Application.Services;

namespace DiscountAggregator.Application.CommandsQueries
{
    public class CollectDiscountsCommand
    {
        private readonly DiscountService _service;
        public CollectDiscountsCommand(DiscountService service)
        {
            _service = service;
        }

        public async Task<int> ExecuteAsync(string keyword, CancellationToken ct = default)
        {
            return await _service.CollectDiscountsAsync(keyword, ct);
        }
    }
}
