using DiscountAggregator.Application.Services;
using DiscountAggregator.Bot.Configuration;
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

                    var keywords = _options.DefaultKeywords?.Length > 0
                        ? _options.DefaultKeywords
                        : new[] { "ноутбук" };

                    foreach (var keyword in keywords)
                    {
                        var count = await collector.CollectDiscountsAsync(keyword, stoppingToken);
                        _logger.LogInformation("Collected {Count} discounts for '{Keyword}'", count, keyword);
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

