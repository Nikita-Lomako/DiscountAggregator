using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DiscountAggregator.Application.DTOs;
using DiscountAggregator.Domain.Entities;

namespace DiscountAggregator.Application.Interfaces
{
    public interface IDiscountSource
    {
        string SourceKey { get; } // "wildberries"
        Task<IEnumerable<Product>> FetchAsync(SourceFetchRequest request, CancellationToken ct = default);
    }
} 