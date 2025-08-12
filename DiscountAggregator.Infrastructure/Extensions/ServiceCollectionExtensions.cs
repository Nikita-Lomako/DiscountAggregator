using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;

namespace DiscountAggregator.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructureLayer(this IServiceCollection services, IConfiguration config)
        {
            var token = config["TelegramBot:Token"];
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Telegram bot token is not configured.");
            services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(token));
            return services;
        }
    }
}
