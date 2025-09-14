using System.Text.Json;
using DiscountAggregator.Domain.Entities;
using DiscountAggregator.Domain.Interfaces;

namespace DiscountAggregator.Infrastructure.Repositories
{
    public class JsonUserSubscriptionRepository : IUserSubscriptionRepository
    {
        private readonly string _filePath;
        private readonly List<UserSubscription> _cache = new();
        private readonly object _lock = new();

        public JsonUserSubscriptionRepository(string filePath)
        {
            _filePath = filePath;
            Load();
        }

        private void Load()
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var items = JsonSerializer.Deserialize<List<UserSubscription>>(json);
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

        public Task UpsertAsync(UserSubscription subscription, CancellationToken ct = default)
        {
            lock (_lock)
            {
                var idx = _cache.FindIndex(x => x.UserId == subscription.UserId && x.ApiSubscriptionId == subscription.ApiSubscriptionId);
                if (idx >= 0) _cache[idx] = subscription; else _cache.Add(subscription);
                Save();
            }
            return Task.CompletedTask;
        }

        public Task<UserSubscription?> GetAsync(long userId, Guid apiSubscriptionId, CancellationToken ct = default)
        {
            lock (_lock)
            {
                var us = _cache.FirstOrDefault(x => x.UserId == userId && x.ApiSubscriptionId == apiSubscriptionId);
                return Task.FromResult(us);
            }
        }

        public Task<IReadOnlyList<UserSubscription>> GetByUserAsync(long userId, CancellationToken ct = default)
        {
            lock (_lock)
            {
                return Task.FromResult((IReadOnlyList<UserSubscription>)_cache.Where(x => x.UserId == userId).ToList());
            }
        }

        public Task<IReadOnlyList<UserSubscription>> GetSubscribedUsersAsync(Guid apiSubscriptionId, CancellationToken ct = default)
        {
            lock (_lock)
            {
                return Task.FromResult((IReadOnlyList<UserSubscription>)_cache.Where(x => x.ApiSubscriptionId == apiSubscriptionId && x.Subscribed).ToList());
            }
        }
    }
}

