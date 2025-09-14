using System.Text.Json;
using DiscountAggregator.Domain.Entities;
using DiscountAggregator.Domain.Interfaces;

namespace DiscountAggregator.Infrastructure.Repositories
{
    public class JsonQueryLogRepository : IQueryLogRepository
    {
        private readonly string _filePath;
        private readonly List<QueryLog> _cache = new();
        private readonly object _lock = new();

        public JsonQueryLogRepository(string filePath)
        {
            _filePath = filePath;
            Load();
        }

        private void Load()
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var items = JsonSerializer.Deserialize<List<QueryLog>>(json);
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

        public Task AddAsync(QueryLog log, CancellationToken ct = default)
        {
            lock (_lock)
            {
                _cache.Add(log);
                Save();
            }
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<QueryLog>> GetRecentAsync(long userId, TimeSpan window, CancellationToken ct = default)
        {
            var since = DateTime.UtcNow - window;
            List<QueryLog> items;
            lock (_lock)
            {
                items = _cache
                    .Where(x => x.UserId == userId && x.QueriedAtUtc >= since)
                    .OrderByDescending(x => x.QueriedAtUtc)
                    .ToList();
            }
            return Task.FromResult((IReadOnlyList<QueryLog>)items);
        }
    }
}

