using DiscountAggregator.Application.Interfaces;
using DiscountAggregator.Application.DTOs;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DiscountAggregator.Infrastructure.Sources.Wildberries
{
    public class WildberriesSource : IDiscountSource
    {
        public string SourceKey => "wildberries";

        public async Task<IEnumerable<RawDiscountDto>> FetchAsync(SourceFetchRequest request, CancellationToken ct = default)
        {
            // Stub: возвращает одну тестовую скидку
            var result = new List<RawDiscountDto>
            {
                new RawDiscountDto
                {
                    ExternalId = "wb-1",
                    Title = "Ноутбук Wildberries",
                    Brand = "WildBrand",
                    Price = 49999,
                    OldPrice = 59999,
                    Url = "https://www.wildberries.ru/product/1"
                }
            };
            return await Task.FromResult(result);
        }
    }
}
