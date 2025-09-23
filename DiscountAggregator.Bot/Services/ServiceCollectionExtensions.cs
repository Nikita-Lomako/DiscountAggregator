using DiscountAggregator.Application.Services;
using DiscountAggregator.Application.CommandsQueries;
using DiscountAggregator.Application.Interfaces;
using DiscountAggregator.Domain.Interfaces;
using DiscountAggregator.Infrastructure.Notifications;
using DiscountAggregator.Infrastructure.Repositories;
using DiscountAggregator.Application.Sources.Wildberries;
using DiscountAggregator.Bot.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using System.Net.Http.Headers;
using System.Net;
using DiscountAggregator.Infrastructure.Extensions;

namespace DiscountAggregator.Bot.Services
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBotServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Конфигурация
            services.Configure<TelegramBotOptions>(configuration.GetSection(TelegramBotOptions.SectionName));
            services.Configure<DataSourceOptions>(configuration.GetSection(DataSourceOptions.SectionName));

            // Telegram Bot
            var botOptions = configuration.GetSection(TelegramBotOptions.SectionName).Get<TelegramBotOptions>();
            var token = botOptions?.Token ?? configuration["TelegramBot:Token"];           
            
            if (string.IsNullOrEmpty(token))
                throw new ArgumentException("Telegram bot token is not configured. Please check appsettings.json or appsettings.Development.json");
            
            services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(token));

            // Источники данных
            services.AddTransient<RetryHandler>();
            services.AddHttpClient("wildberries", c =>
            {
                c.BaseAddress = new Uri("https://www.wildberries.ru/");
                c.Timeout = TimeSpan.FromSeconds(15);
                c.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36");
                c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
                c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xhtml+xml", 0.9));
                c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml", 0.8));
                c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/avif", 0.8));
                c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/webp", 0.8));
                c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*", 0.7));
                c.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("ru-RU"));
                c.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("ru"));
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                var handler = new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.All,
                    UseCookies = true,
                    CookieContainer = new CookieContainer(),
                    AllowAutoRedirect = true
                };
                return handler;
            })
            .AddHttpMessageHandler<RetryHandler>();

            services.AddSingleton<IDiscountSource, WildberriesSourcePlaywright>();

            // Подключаем инфраструктуру (регистрирует EF репозитории при наличии строки подключения)
            services.AddInfrastructureLayer(configuration);

            // Сервисы приложения
            services.AddScoped<IProductCacheService, RedisProductCacheService>();
            services.AddScoped<DiscountService>();
            services.AddScoped<CollectDiscountsCommand>();

            // Уведомления
            services.AddSingleton<INotificationService>(provider =>
            {
                var botClient = provider.GetRequiredService<ITelegramBotClient>();
                return new TelegramNotifier(botClient);
            });

            // Фоновый сборщик (простой IHostedService)
            services.AddHostedService<DiscountCollectorHostedService>();

            return services;
        }
    }
} 