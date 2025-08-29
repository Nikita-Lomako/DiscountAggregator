using DiscountAggregator.Application.Services;
using DiscountAggregator.Application.CommandsQueries;
using DiscountAggregator.Application.Interfaces;
using DiscountAggregator.Domain.Interfaces;
using DiscountAggregator.Infrastructure.Notifications;
using DiscountAggregator.Infrastructure.Repositories;
using DiscountAggregator.Infrastructure.Sources.Wildberries;
using DiscountAggregator.Bot.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;

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
            services.AddSingleton<IDiscountSource, WildberriesSource>();

            // Репозитории
            var dataSourceOptions = configuration.GetSection(DataSourceOptions.SectionName).Get<DataSourceOptions>();
            services.AddSingleton<IDiscountRepository>(provider => 
                new JsonDiscountRepository(dataSourceOptions?.JsonFilePath ?? "discounts.json"));

            // Сервисы приложения
            services.AddScoped<DiscountService>();
            services.AddScoped<CollectDiscountsCommand>();

            // Уведомления
            services.AddSingleton<INotificationService>(provider =>
            {
                var botClient = provider.GetRequiredService<ITelegramBotClient>();
                return new TelegramNotifier(botClient);
            });

            return services;
        }
    }
} 