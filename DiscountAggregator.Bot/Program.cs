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
                    // Готовим абсолютные пути к папке Infrastructure для логов
                    string baseDir = AppContext.BaseDirectory; // ...\Bot\bin\Debug\net8.0
                    var infraPath = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..", "DiscountAggregator", "DiscountAggregator.Infrastructure"));
                    var infraLogs = Path.Combine(infraPath, "logs");
                    Directory.CreateDirectory(infraLogs);
                    loggerConfiguration
                        .ReadFrom.Configuration(context.Configuration)
                        .ReadFrom.Services(services)
                        .Enrich.FromLogContext()
                        .WriteTo.File(Path.Combine(infraLogs, "discount-aggregator-.log"), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7);
                })
                .ConfigureServices((context, services) =>
                {
                    // Ничего не делаем здесь с рабочей директорией
                    // Регистрируем все сервисы
                    services.AddBotServices(context.Configuration);

                    // Регистрируем Telegram Bot Hosted Service
                    services.AddHostedService<TelegramBotHostedService>();
                });
    }
}

