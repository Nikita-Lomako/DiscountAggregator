using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DiscountAggregator.Application.DTOs;

namespace DiscountAggregator.Application.Interfaces
{
    public interface IDiscountSource
    {
        string SourceKey { get; } // "wildberries"
        Task<IEnumerable<RawDiscountDto>> FetchAsync(SourceFetchRequest request, CancellationToken ct = default);
    }
} 