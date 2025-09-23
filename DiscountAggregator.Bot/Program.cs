using DiscountAggregator.Bot.Services;
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
                 .UseSerilog((context, services, loggerConfiguration) =>
                {
                    // Создаем папку для логов в рабочей директории приложения
                    var logsDir = Path.Combine(AppContext.BaseDirectory, "logs");
                    Directory.CreateDirectory(logsDir);
                    
                    loggerConfiguration
                        .ReadFrom.Configuration(context.Configuration)
                        .ReadFrom.Services(services)
                        .Enrich.FromLogContext()
                        .WriteTo.File(Path.Combine(logsDir, "discount-aggregator-.log"), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7);
                })
                .ConfigureServices((context, services) =>
                {
                    // Ничего не делаем здесь с рабочей директорией
                    // Регистрируем все сервисы
                    services.AddStackExchangeRedisCache(options =>
                    {
                        options.Configuration = context.Configuration["Redis:Configuration"];
                        options.InstanceName = "DiscountAggregator_";
                    });
                    services.AddBotServices(context.Configuration);

                    // Регистрируем Telegram Bot Hosted Service
                    services.AddHostedService<TelegramBotHostedService>();
                });
    }
}

