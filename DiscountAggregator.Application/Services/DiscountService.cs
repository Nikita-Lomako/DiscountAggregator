using DiscountAggregator.Domain.Entities;
using DiscountAggregator.Domain.Interfaces;
using DiscountAggregator.Application.Interfaces;
using DiscountAggregator.Application.DTOs;
using Serilog;

namespace DiscountAggregator.Application.Services
{
    public class DiscountService
    {
        private readonly IDiscountSource _source;
        private readonly IProductRepository _productRepository;
        private readonly IProductPriceHistoryRepository _priceHistoryRepository;
        
        public DiscountService(IDiscountSource source, IProductRepository productRepository, IProductPriceHistoryRepository priceHistoryRepository)
        {
            _source = source;
            _productRepository = productRepository;
            _priceHistoryRepository = priceHistoryRepository;
        }

        public async Task<int> CollectDiscountsAsync(string keyword, CancellationToken ct = default)
        {
            var request = new SourceFetchRequest { Keyword = keyword, Limit = 30 };
            Log.Information("CollectDiscounts: fetching from source '{SourceKey}' for '{Keyword}' with limit {Limit}", _source.SourceKey, keyword, request.Limit);
            var products = await _source.FetchAsync(request, ct);
            int count = 0;
            foreach (var product in products)
            {
                await UpsertProductWithPriceHistoryAsync(product, ct);
                count++;
            }
            Log.Information("CollectDiscounts: upserted {Count} items for '{Keyword}'", count, keyword);
            return count;
        }

        public async Task<IEnumerable<Product>> GetOrCollectAsync(string keyword, TimeSpan cacheTtl, CancellationToken ct = default)
        {
            var since = DateTime.UtcNow - cacheTtl;
            Log.Information("GetOrCollect: checking cache for '{Keyword}' since {SinceUtc} using {Repository}", keyword, since, _productRepository.GetType().Name);
            var recent = await _productRepository.SearchSinceAsync(keyword, since, ct);
            if (recent.Any())
            {
                Log.Information("GetOrCollect: cache hit for '{Keyword}', returning {Count} items", keyword, recent.Count());
                return recent;
            }

            Log.Information("GetOrCollect: cache miss for '{Keyword}', invoking source fetch", keyword);
            await CollectDiscountsAsync(keyword, ct);
            var after = await _productRepository.SearchSinceAsync(keyword, since, ct);
            Log.Information("GetOrCollect: after fetch, repository returned {Count} items for '{Keyword}'", after.Count(), keyword);
            return after;
        }

        private async Task UpsertProductWithPriceHistoryAsync(Product product, CancellationToken ct = default)
        {
            var existing = await _productRepository.GetBySourceAndExternalIdAsync(product.Source, product.ExternalId, ct);
            
            if (existing == null)
            {
                // Новый продукт - просто сохраняем
                await _productRepository.UpsertAsync(product, ct);
                await _priceHistoryRepository.AddPriceRecordAsync(product.Id, product.CurrentPrice, DateTime.UtcNow, ct);
            }
            else
            {
                // Существующий продукт - проверяем изменение цены
                var priceChanged = existing.CurrentPrice != product.CurrentPrice;
                
                // Обновляем продукт
                existing.Title = product.Title;
                existing.Brand = product.Brand;
                existing.CurrentPrice = product.CurrentPrice;
                existing.OldPrice = product.OldPrice;
                existing.Url = product.Url;
                existing.LastUpdatedAtUtc = DateTime.UtcNow;
                
                await _productRepository.UpsertAsync(existing, ct);
                
                // Добавляем запись в историю цен если цена изменилась
                if (priceChanged)
                {
                    await _priceHistoryRepository.AddPriceRecordAsync(existing.Id, product.CurrentPrice, DateTime.UtcNow, ct);
                }
            }
        }
    }
}
