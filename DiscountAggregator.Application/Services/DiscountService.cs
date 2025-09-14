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
        private readonly IDiscountRepository _repository;
        public DiscountService(IDiscountSource source, IDiscountRepository repository)
        {
            _source = source;
            _repository = repository;
        }

        public async Task<int> CollectDiscountsAsync(string keyword, CancellationToken ct = default)
        {
            var request = new SourceFetchRequest { Keyword = keyword, Limit = 30 };
            Log.Information("CollectDiscounts: fetching from source '{SourceKey}' for '{Keyword}' with limit {Limit}", _source.SourceKey, keyword, request.Limit);
            var rawDiscounts = await _source.FetchAsync(request, ct);
            int count = 0;
            foreach (var raw in rawDiscounts)
            {
                var discount = Normalize(raw, _source.SourceKey);
                await _repository.UpsertAsync(discount, ct);
                count++;
            }
            Log.Information("CollectDiscounts: upserted {Count} items for '{Keyword}'", count, keyword);
            return count;
        }

        public async Task<IEnumerable<Discount>> GetOrCollectAsync(string keyword, TimeSpan cacheTtl, CancellationToken ct = default)
        {
            var since = DateTime.UtcNow - cacheTtl;
            Log.Information("GetOrCollect: checking cache for '{Keyword}' since {SinceUtc} using {Repository}", keyword, since, _repository.GetType().Name);
            var recent = await _repository.SearchSinceAsync(keyword, since, ct);
            if (recent.Any())
            {
                Log.Information("GetOrCollect: cache hit for '{Keyword}', returning {Count} items", keyword, recent.Count());
                return recent;
            }

            Log.Information("GetOrCollect: cache miss for '{Keyword}', invoking source fetch", keyword);
            await CollectDiscountsAsync(keyword, ct);
            var after = await _repository.SearchSinceAsync(keyword, since, ct);
            Log.Information("GetOrCollect: after fetch, repository returned {Count} items for '{Keyword}'", after.Count(), keyword);
            return after;
        }

        private Discount Normalize(RawDiscountDto raw, string sourceKey)
        {
            return new Discount
            {
                Id = Guid.NewGuid(),
                Source = sourceKey,
                ExternalId = raw.ExternalId,
                Title = raw.Title,
                Brand = raw.Brand,
                Price = raw.Price,
                OldPrice = raw.OldPrice,
                Url = raw.Url,
                FetchedAtUtc = DateTime.UtcNow,
                Fingerprint = GenerateFingerprint(sourceKey, raw.ExternalId)
            };
        }

        private string GenerateFingerprint(string source, string externalId)
        {
            return $"{source}:{externalId}".ToLowerInvariant();
        }
    }
}
