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
        private readonly IProductCacheService _cacheService;
        
        public DiscountService(IDiscountSource source, IProductRepository productRepository, IProductPriceHistoryRepository priceHistoryRepository, IProductCacheService cacheService)
        {
            _source = source;
            _productRepository = productRepository;
            _priceHistoryRepository = priceHistoryRepository;
            _cacheService = cacheService;
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
            Log.Information("GetOrCollect: checking Redis cache for '{Keyword}'", keyword);
            
            // Сначала проверяем Redis кеш
            var cachedProducts = await _cacheService.GetCachedProductsAsync(keyword, ct);
            if (cachedProducts.Any())
            {
                Log.Information("GetOrCollect: Redis cache hit for '{Keyword}', returning {Count} items", keyword, cachedProducts.Count());
                return cachedProducts;
            }

            Log.Information("GetOrCollect: Redis cache miss for '{Keyword}', fetching from source", keyword);
            
            // Если в кеше нет, получаем данные из источника
            var request = new SourceFetchRequest { Keyword = keyword, Limit = 30 };
            var products = await _source.FetchAsync(request, ct);
            var productsList = products.ToList();
            
            // Сохраняем в Redis кеш
            await _cacheService.SetCachedProductsAsync(keyword, productsList, cacheTtl, ct);
            
            Log.Information("GetOrCollect: fetched and cached {Count} items for '{Keyword}'", productsList.Count, keyword);
            return productsList;
        }

        public async Task SaveProductsToDatabaseAsync(string keyword, CancellationToken ct = default)
        {
            Log.Information("SaveProductsToDatabase: saving products for '{Keyword}' to database", keyword);
            
            // Получаем товары из кеша
            var cachedProducts = await _cacheService.GetCachedProductsAsync(keyword, ct);
            var productsList = cachedProducts.ToList();
            
            if (!productsList.Any())
            {
                Log.Warning("SaveProductsToDatabase: no cached products found for '{Keyword}'", keyword);
                return;
            }

            // Сохраняем каждый товар в БД
            int savedCount = 0;
            foreach (var product in productsList)
            {
                await UpsertProductWithPriceHistoryAsync(product, ct);
                savedCount++;
            }
            
            Log.Information("SaveProductsToDatabase: saved {Count} products for '{Keyword}' to database", savedCount, keyword);
        }

        public async Task<int> DeleteProductsByKeywordAsync(string keyword, CancellationToken ct = default)
        {
            Log.Information("DeleteProductsByKeyword: deleting products for '{Keyword}'", keyword);
            
            // Получаем товары для удаления, чтобы получить их ID для удаления истории цен
            var productsToDelete = await _productRepository.SearchAsync(keyword, ct);
            var productIds = productsToDelete.Select(p => p.Id).ToList();
            
            // Удаляем историю цен для этих товаров
            int deletedHistoryCount = 0;
            if (productIds.Any())
            {
                deletedHistoryCount = await _priceHistoryRepository.DeleteByProductIdsAsync(productIds, ct);
                Log.Information("DeleteProductsByKeyword: deleted {Count} price history records for '{Keyword}'", deletedHistoryCount, keyword);
            }
            
            // Удаляем товары
            int deletedProductsCount = await _productRepository.DeleteByKeywordAsync(keyword, ct);
            Log.Information("DeleteProductsByKeyword: deleted {Count} products for '{Keyword}'", deletedProductsCount, keyword);
            
            // Очищаем кеш
            await _cacheService.ClearCacheAsync(keyword, ct);
            Log.Information("DeleteProductsByKeyword: cleared cache for '{Keyword}'", keyword);
            
            return deletedProductsCount;
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
