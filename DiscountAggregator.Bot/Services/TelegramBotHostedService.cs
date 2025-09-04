using DiscountAggregator.Application.Services;
using DiscountAggregator.Bot.Configuration;
using DiscountAggregator.Bot.Logging;
using DiscountAggregator.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace DiscountAggregator.Bot.Services
{
    public class TelegramBotHostedService : BackgroundService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ILogger<TelegramBotHostedService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TelegramBotOptions _botOptions;

        public TelegramBotHostedService(
            ITelegramBotClient botClient,
            ILogger<TelegramBotHostedService> logger,
            IServiceProvider serviceProvider,
            IOptions<TelegramBotOptions> botOptions)
        {
            _botClient = botClient;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _botOptions = botOptions.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting Telegram Bot...");

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = _botOptions.AllowedUpdates.Select(u => Enum.Parse<UpdateType>(u)).ToArray()
            };

            _botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                errorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: stoppingToken
            );

            _logger.LogInformation("Bot started successfully");

            // Ждем отмены
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type != UpdateType.Message || update.Message?.Text is null)
                return;

            var message = update.Message;
            var userId = message.Chat.Id;
            var text = message.Text;

            _logger.LogInformation("Received message from {UserId}: {Text}", userId, text);

            using var scope = _serviceProvider.CreateScope();
            var discountService = scope.ServiceProvider.GetRequiredService<DiscountService>();
            var repository = scope.ServiceProvider.GetRequiredService<IDiscountRepository>();
            var notifier = scope.ServiceProvider.GetRequiredService<INotificationService>();

            try
            {
                if (text.StartsWith("/start"))
                {
                    await botClient.SendMessage(
                        chatId: userId,
                        text: "Добро пожаловать! Используйте /search <ключевое_слово> для поиска скидок.",
                        cancellationToken: cancellationToken
                    );
                }
                else if (text.StartsWith("/search "))
                {
                    var keyword = text.Substring(8).Trim();
                    if (string.IsNullOrWhiteSpace(keyword))
                    {
                        await botClient.SendMessage(
                            chatId: userId,
                            text: "Пожалуйста, укажите ключевое слово: /search ноутбук",
                            cancellationToken: cancellationToken
                        );
                        return;
                    }

                    await botClient.SendMessage(
                        chatId: userId,
                        text: $"Ищу скидки по запросу: {keyword}...",
                        cancellationToken: cancellationToken
                    );

                    var discounts = await discountService.GetOrCollectAsync(keyword, TimeSpan.FromHours(1), cancellationToken);

                    if (!discounts.Any())
                    {
                        await botClient.SendMessage(
                            chatId: userId,
                            text: "Скидки не найдены.",
                            cancellationToken: cancellationToken
                        );
                        return;
                    }

                    var combined = string.Join("\n\n", discounts.Take(10).Select(discount =>
                        $"{discount.Title}\nБренд: {discount.Brand}\nЦена: {discount.Price} (было {discount.OldPrice})\nСкидка: {discount.DiscountPercent}%\nСсылка: {discount.Url}"));
                    await notifier.NotifyAsync(userId, combined, cancellationToken);
                }
                else
                {
                    await botClient.SendMessage(
                        chatId: userId,
                        text: "Неизвестная команда. Используйте /search <ключевое_слово>.",
                        cancellationToken: cancellationToken
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling message from {UserId}", userId);
                await botClient.SendMessage(
                    chatId: userId,
                    text: "Произошла ошибка при обработке вашего запроса.",
                    cancellationToken: cancellationToken
                );
            }
        }

        private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            _logger.LogError(exception, "Telegram polling error: {ErrorMessage}", errorMessage);
            return Task.CompletedTask;
        }
    }
} 