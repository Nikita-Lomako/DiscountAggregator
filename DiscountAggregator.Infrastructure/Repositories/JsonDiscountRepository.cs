using System.Text.Json;
using DiscountAggregator.Domain.Entities;
using DiscountAggregator.Domain.Interfaces;

namespace DiscountAggregator.Infrastructure.Repositories
{
    public class JsonDiscountRepository : IDiscountRepository
    {
        private readonly string _filePath;
        private readonly List<Discount> _cache = new();
        private readonly object _lock = new();

        public JsonDiscountRepository(string filePath)
        {
            _filePath = filePath;
            Load();
        }

        private void Load()
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var discounts = JsonSerializer.Deserialize<List<Discount>>(json);
                if (discounts != null)
                    _cache.AddRange(discounts);
            }
        }

        private void Save()
        {
            var json = JsonSerializer.Serialize(_cache);
            File.WriteAllText(_filePath, json);
        }

        public async Task UpsertAsync(Discount discount, CancellationToken ct = default)
        {
            lock (_lock)
            {
                var idx = _cache.FindIndex(d => d.Fingerprint == discount.Fingerprint);
                if (idx >= 0)
                    _cache[idx] = discount;
                else
                    _cache.Add(discount);
                Save();
            }
            await Task.CompletedTask;
        }

        public async Task<IEnumerable<Discount>> SearchAsync(string keyword, CancellationToken ct = default)
        {
            IEnumerable<Discount> result;
            lock (_lock)
            {
                result = _cache.Where(d => d.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                                           d.Brand.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            return await Task.FromResult(result);
        }

        public async Task<IEnumerable<Discount>> GetRecentAsync(int hours, CancellationToken ct = default)
        {
            var threshold = DateTime.UtcNow.AddHours(-hours);
            IEnumerable<Discount> result;
            lock (_lock)
            {
                result = _cache.Where(d => d.FetchedAtUtc >= threshold).ToList();
            }
            return await Task.FromResult(result);
        }

        public async Task<IEnumerable<Discount>> SearchSinceAsync(string keyword, DateTime sinceUtc, CancellationToken ct = default)
        {
            IEnumerable<Discount> result;
            lock (_lock)
            {
                result = _cache
                    .Where(d => d.FetchedAtUtc >= sinceUtc)
                    .Where(d => d.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                                d.Brand.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            return await Task.FromResult(result);
        }
    }
}
