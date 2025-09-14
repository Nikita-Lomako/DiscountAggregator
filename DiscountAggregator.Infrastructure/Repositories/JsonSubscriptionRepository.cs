using System.Text.Json;
using DiscountAggregator.Domain.Entities;
using DiscountAggregator.Domain.Interfaces;

namespace DiscountAggregator.Infrastructure.Repositories
{
    public class JsonSubscriptionRepository : ISubscriptionRepository
    {
        private readonly string _filePath;
        private readonly List<Subscription> _cache = new();
        private readonly object _lock = new();

        public JsonSubscriptionRepository(string filePath)
        {
            _filePath = filePath;
            Load();
        }

        private void Load()
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var items = JsonSerializer.Deserialize<List<Subscription>>(json);
                if (items != null)
                    _cache.AddRange(items);
            }
        }

        private void Save()
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(_cache);
            File.WriteAllText(_filePath, json);
        }

        public Task AddAsync(Subscription subscription, CancellationToken ct = default)
        {
            lock (_lock)
            {
                if (!_cache.Any(s => s.UserId == subscription.UserId && s.Keyword.Equals(subscription.Keyword, StringComparison.OrdinalIgnoreCase)))
                {
                    _cache.Add(subscription);
                    Save();
                }
            }
            return Task.CompletedTask;
        }

        public Task RemoveAsync(long userId, string keyword, CancellationToken ct = default)
        {
            lock (_lock)
            {
                _cache.RemoveAll(s => s.UserId == userId && s.Keyword.Equals(keyword, StringComparison.OrdinalIgnoreCase));
                Save();
            }
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(long userId, string keyword, CancellationToken ct = default)
        {
            bool exists;
            lock (_lock)
            {
                exists = _cache.Any(s => s.UserId == userId && s.Keyword.Equals(keyword, StringComparison.OrdinalIgnoreCase));
            }
            return Task.FromResult(exists);
        }

        public Task<IReadOnlyList<Subscription>> GetByUserAsync(long userId, CancellationToken ct = default)
        {
            List<Subscription> items;
            lock (_lock)
            {
                items = _cache.Where(s => s.UserId == userId).OrderByDescending(s => s.SubscribedAtUtc).ToList();
            }
            return Task.FromResult((IReadOnlyList<Subscription>)items);
        }
    }
}

