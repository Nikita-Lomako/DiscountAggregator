using DiscountAggregator.Domain.Interfaces;
using DiscountAggregator.Infrastructure.Data;
using DiscountAggregator.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;

namespace DiscountAggregator.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructureLayer(this IServiceCollection services, IConfiguration config)
        {
            // Telegram client (optional for infra consumers)
            var token = config["TelegramBot:Token"];
            if (!string.IsNullOrWhiteSpace(token))
            {
                services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(token));
            }

            // EF Core DbContext if connection string is provided
            var connectionString = config.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                services.AddDbContext<AppDbContext>(options =>
                    options.UseNpgsql(connectionString));

                // Register EF-based repositories
                services.AddScoped<IProductRepository, DbProductRepository>();
                services.AddScoped<IUserRepository, DbUserRepository>();
                services.AddScoped<IUserProductSubscriptionRepository, DbUserProductSubscriptionRepository>();
                services.AddScoped<IProductPriceHistoryRepository, DbProductPriceHistoryRepository>();
                services.AddScoped<ISearchQueryRepository, DbSearchQueryRepository>();
            }
            return services;
        }
    }
}
