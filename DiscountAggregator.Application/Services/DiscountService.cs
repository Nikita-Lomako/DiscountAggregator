using DiscountAggregator.Domain.Entities;
using DiscountAggregator.Domain.Interfaces;
using DiscountAggregator.Application.Interfaces;
using DiscountAggregator.Application.DTOs;

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
            var request = new SourceFetchRequest { Keyword = keyword, Limit = 10 };
            var rawDiscounts = await _source.FetchAsync(request, ct);
            int count = 0;
            foreach (var raw in rawDiscounts)
            {
                var discount = Normalize(raw, _source.SourceKey);
                await _repository.UpsertAsync(discount, ct);
                count++;
            }
            return count;
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
