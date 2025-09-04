using System.Text.Json;
using DiscountAggregator.Domain.Entities;
using DiscountAggregator.Domain.Interfaces;

namespace DiscountAggregator.Infrastructure.Repositories
{
    public class JsonApiSubscriptionRepository : IApiSubscriptionRepository
    {
        private readonly string _filePath;
        private readonly List<ApiSubscription> _cache = new();
        private readonly object _lock = new();

        public JsonApiSubscriptionRepository(string filePath)
        {
            _filePath = filePath;
            Load();
        }

        private void Load()
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var items = JsonSerializer.Deserialize<List<ApiSubscription>>(json);
                if (items != null) _cache.AddRange(items);
            }
        }

        private void Save()
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(_cache);
            File.WriteAllText(_filePath, json);
        }

        public Task<ApiSubscription> GetOrCreateAsync(string sourceKey, string keyword, CancellationToken ct = default)
        {
            lock (_lock)
            {
                var existing = _cache.FirstOrDefault(x => x.SourceKey.Equals(sourceKey, StringComparison.OrdinalIgnoreCase) && x.Keyword.Equals(keyword, StringComparison.OrdinalIgnoreCase));
                if (existing != null) return Task.FromResult(existing);
                var created = new ApiSubscription
                {
                    Id = Guid.NewGuid(),
                    SourceKey = sourceKey,
                    Keyword = keyword,
                    CreatedAtUtc = DateTime.UtcNow
                };
                _cache.Add(created);
                Save();
                return Task.FromResult(created);
            }
        }

        public Task<ApiSubscription?> GetAsync(string sourceKey, string keyword, CancellationToken ct = default)
        {
            lock (_lock)
            {
                var existing = _cache.FirstOrDefault(x => x.SourceKey.Equals(sourceKey, StringComparison.OrdinalIgnoreCase) && x.Keyword.Equals(keyword, StringComparison.OrdinalIgnoreCase));
                return Task.FromResult(existing);
            }
        }

        public Task<IReadOnlyList<ApiSubscription>> GetAllAsync(CancellationToken ct = default)
        {
            lock (_lock)
            {
                return Task.FromResult((IReadOnlyList<ApiSubscription>)_cache.ToList());
            }
        }
    }
}

