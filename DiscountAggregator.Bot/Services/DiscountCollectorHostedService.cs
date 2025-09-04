using DiscountAggregator.Application.Services;
using DiscountAggregator.Bot.Configuration;
using DiscountAggregator.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiscountAggregator.Bot.Services
{
    public class DiscountCollectorHostedService : BackgroundService
    {
        private readonly ILogger<DiscountCollectorHostedService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly DataSourceOptions _options;

        public DiscountCollectorHostedService(
            ILogger<DiscountCollectorHostedService> logger,
            IServiceProvider serviceProvider,
            IOptions<DataSourceOptions> options)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var delay = TimeSpan.FromSeconds(Math.Max(30, _options.CollectorIntervalSeconds));
            _logger.LogInformation("DiscountCollector started. Interval: {Delay}s", delay.TotalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var collector = scope.ServiceProvider.GetRequiredService<DiscountService>();
                    var subs = scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();

                    var allKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    // collect from user subscriptions
                    // naive scan over all users from repository is not available with current interface;
                    // here we fall back to DefaultKeywords; extend later with user registry if needed
                    var keywords = _options.DefaultKeywords?.Length > 0
                        ? _options.DefaultKeywords
                        : new[] { "ноутбук" };
                    foreach (var k in keywords) allKeywords.Add(k);

                    foreach (var keyword in allKeywords)
                    {
                        var items = await collector.GetOrCollectAsync(keyword, TimeSpan.FromHours(1), stoppingToken);
                        var count = items.Count();
                        _logger.LogInformation("Collected or reused {Count} discounts for '{Keyword}'", count, keyword);
                        await Task.Delay(TimeSpan.FromMilliseconds(Random.Shared.Next(300, 800)), stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Collector iteration failed");
                }

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}

