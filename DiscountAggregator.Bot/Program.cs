using DiscountAggregator.Bot.Services;
using DiscountAggregator.Infrastructure.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace DiscountAggregator.Bot
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Настройка Serilog
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateBootstrapLogger();

            try
            {
                Log.Information("Starting Discount Aggregator Bot...");

                var host = CreateHostBuilder(args).Build();
                await host.RunAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog((context, services, configuration) => 
                    LoggingConfiguration.CreateLogger(context.Configuration))
                .ConfigureServices((context, services) =>
                {
                    // Регистрируем все сервисы
                    services.AddBotServices(context.Configuration);
                    
                    // Регистрируем Telegram Bot Hosted Service
                    services.AddHostedService<TelegramBotHostedService>();
                });
    }
}