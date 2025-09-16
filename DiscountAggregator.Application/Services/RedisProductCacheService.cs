using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DiscountAggregator.Application.Interfaces;
using DiscountAggregator.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;
using Serilog;

namespace DiscountAggregator.Application.Services
{
    public class RedisProductCacheService : IProductCacheService
    {
        private readonly IDistributedCache _cache;
        private readonly JsonSerializerOptions _jsonOptions;

        public RedisProductCacheService(IDistributedCache cache)
        {
            _cache = cache;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        public async Task<IEnumerable<Product>> GetCachedProductsAsync(string keyword, CancellationToken ct = default)
        {
            try
            {
                var cacheKey = GetCacheKey(keyword);
                var cachedData = await _cache.GetStringAsync(cacheKey, ct);
                
                if (string.IsNullOrEmpty(cachedData))
                {
                    Log.Information("Cache miss for keyword: {Keyword}", keyword);
                    return new List<Product>();
                }

                var products = JsonSerializer.Deserialize<List<Product>>(cachedData, _jsonOptions);
                Log.Information("Cache hit for keyword: {Keyword}, found {Count} products", keyword, products?.Count ?? 0);
                return products ?? new List<Product>();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting cached products for keyword: {Keyword}", keyword);
                return new List<Product>();
            }
        }

        public async Task SetCachedProductsAsync(string keyword, IEnumerable<Product> products, TimeSpan expiration, CancellationToken ct = default)
        {
            try
            {
                var cacheKey = GetCacheKey(keyword);
                var jsonData = JsonSerializer.Serialize(products, _jsonOptions);
                
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration
                };

                await _cache.SetStringAsync(cacheKey, jsonData, options, ct);
                Log.Information("Cached {Count} products for keyword: {Keyword} with expiration: {Expiration}", 
                    products.Count(), keyword, expiration);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error caching products for keyword: {Keyword}", keyword);
            }
        }

        public async Task<bool> IsCachedAsync(string keyword, CancellationToken ct = default)
        {
            try
            {
                var cacheKey = GetCacheKey(keyword);
                var cachedData = await _cache.GetStringAsync(cacheKey, ct);
                return !string.IsNullOrEmpty(cachedData);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error checking cache for keyword: {Keyword}", keyword);
                return false;
            }
        }

        public async Task ClearCacheAsync(string keyword, CancellationToken ct = default)
        {
            try
            {
                var cacheKey = GetCacheKey(keyword);
                await _cache.RemoveAsync(cacheKey, ct);
                Log.Information("Cleared cache for keyword: {Keyword}", keyword);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error clearing cache for keyword: {Keyword}", keyword);
            }
        }

        private static string GetCacheKey(string keyword)
        {
            return $"products:{keyword.ToLowerInvariant()}";
        }
    }
}