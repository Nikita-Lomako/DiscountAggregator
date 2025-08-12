using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DiscountAggregator.Domain.Interfaces
{
    public interface IDiscountSource
    {
        string SourceKey { get; } // "wildberries"
        Task<IEnumerable<RawDiscountDto>> FetchAsync(SourceFetchRequest request, CancellationToken ct = default);
    }

    // DTOs for source fetch (minimal stub for now)
    public class SourceFetchRequest
    {
        public int Limit { get; set; }
        public string Keyword { get; set; } = string.Empty;
    }

    public class RawDiscountDto
    {
        public string ExternalId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal OldPrice { get; set; }
        public string Url { get; set; } = string.Empty;
    }
}
