using DiscountAggregator.Application.Services;
using DiscountAggregator.Bot.Configuration;
using DiscountAggregator.Bot.Logging;
using DiscountAggregator.Domain.Interfaces;
using DiscountAggregator.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

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
            if (update.Type == UpdateType.CallbackQuery)
            {
                await HandleCallback(update.CallbackQuery!, cancellationToken);
                return;
            }

            if (update.Type != UpdateType.Message || update.Message?.Text is null)
                return;

            var message = update.Message;
            var userId = message.Chat.Id;
            var text = message.Text;

            _logger.LogInformation("Received message from {UserId}: {Text}", userId, text);

            using var scope = _serviceProvider.CreateScope();
            var discountService = scope.ServiceProvider.GetRequiredService<DiscountService>();
            var repository = scope.ServiceProvider.GetRequiredService<IDiscountRepository>();
            var subsRepo = scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();
            var queryLog = scope.ServiceProvider.GetRequiredService<IQueryLogRepository>();
            var apiRepo = scope.ServiceProvider.GetRequiredService<IApiSubscriptionRepository>();
            var userApiRepo = scope.ServiceProvider.GetRequiredService<IUserSubscriptionRepository>();
            var notifier = scope.ServiceProvider.GetRequiredService<INotificationService>();

            try
            {
                if (text.StartsWith("/start"))
                {
                    await botClient.SendMessage(
                        chatId: userId,
                        text: "Добро пожаловать! Используйте /search <ключевое_слово> для поиска скидок. Команды: /subscribe, /info",
                        cancellationToken: cancellationToken
                    );
                }
                else if (text.StartsWith("/info"))
                {
                    var commands = "/search <ключевое_слово> — поиск скидок\n/subscribe — список подписок и управление\n/recent — недавние запросы (за час)\n/info — список команд";
                    await botClient.SendMessage(chatId: userId, text: commands, cancellationToken: cancellationToken);
                }
                else if (text.StartsWith("/subscribe"))
                {
                    var userApi = await userApiRepo.GetByUserAsync(userId, cancellationToken);
                    var active = userApi.Where(u => u.Subscribed).ToList();
                    if (active.Count == 0)
                    {
                        await botClient.SendMessage(userId, "У вас нет подписок. Введите /search <ключевое_слово> и подпишитесь из сообщения.", cancellationToken: cancellationToken);
                    }
                    else
                    {
                        var apis = (await apiRepo.GetAllAsync(cancellationToken)).ToDictionary(a => a.Id, a => a);
                        var lines = active
                            .Select(us => apis.TryGetValue(us.ApiSubscriptionId, out var a)
                                ? $"- {a.SourceKey}:{a.Keyword}"
                                : $"- <неизвестный API> {us.ApiSubscriptionId}")
                            .ToList();
                        await botClient.SendMessage(userId, "Ваши активные подписки:\n" + string.Join("\n", lines), cancellationToken: cancellationToken);
                    }
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

                    // пока одна платформа wildberries
                    var sourceKey = "wildberries";
                    var api = await apiRepo.GetOrCreateAsync(sourceKey, keyword, cancellationToken);
                    var existingUserApi = await userApiRepo.GetAsync(userId, api.Id, cancellationToken);
                    if (existingUserApi is null)
                    {
                        await userApiRepo.UpsertAsync(new UserSubscription
                        {
                            Id = Guid.NewGuid(),
                            UserId = userId,
                            ApiSubscriptionId = api.Id,
                            Subscribed = false,
                            UpdatedAtUtc = DateTime.UtcNow
                        }, cancellationToken);
                    }

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

                    await queryLog.AddAsync(new QueryLog { Id = Guid.NewGuid(), UserId = userId, Keyword = keyword, QueriedAtUtc = DateTime.UtcNow }, cancellationToken);
                    var page = 1;
                    await SendPagedResults(userId, sourceKey, keyword, discounts.ToList(), page, cancellationToken);
                }
                else if (text.StartsWith("/recent"))
                {
                    var logs = await queryLog.GetRecentAsync(userId, TimeSpan.FromHours(1), cancellationToken);
                    var keywords = logs.Select(l => l.Keyword).Distinct().Take(10).ToList();
                    if (keywords.Count == 0)
                    {
                        await botClient.SendMessage(userId, "Нет недавних запросов за последний час.", cancellationToken: cancellationToken);
                    }
                    else
                    {
                        var textOut = "Недавние запросы за час:\n" + string.Join("\n", keywords.Select(k => "- " + k));
                        await botClient.SendMessage(userId, textOut, cancellationToken: cancellationToken);
                    }
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

        private async Task SendPagedResults(long userId, string sourceKey, string keyword, List<Domain.Entities.Discount> items, int page, CancellationToken ct, int? messageId = null)
        {
            const int pageSize = 10;
            var totalPages = Math.Max(1, (int)Math.Ceiling(items.Count / (double)pageSize));
            page = Math.Clamp(page, 1, totalPages);

            var slice = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            var text = string.Join("\n\n", slice.Select(d => $"{d.Title}\nБренд: {d.Brand}\nЦена: {d.Price} (было {d.OldPrice})\nСкидка: {d.DiscountPercent}%\nСсылка: {d.Url}"));

            var inline = new List<List<InlineKeyboardButton>>();
            var navRow = new List<InlineKeyboardButton>();
            if (page > 1) navRow.Add(InlineKeyboardButton.WithCallbackData("⬅️", $"page:{sourceKey}:{keyword}:{page - 1}"));
            navRow.Add(InlineKeyboardButton.WithCallbackData($"{page}/{totalPages}", "noop"));
            if (page < totalPages) navRow.Add(InlineKeyboardButton.WithCallbackData("➡️", $"page:{sourceKey}:{keyword}:{page + 1}"));
            inline.Add(navRow);

            inline.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("Подписаться", $"sub:{sourceKey}:{keyword}"),
                InlineKeyboardButton.WithCallbackData("Отписаться", $"unsub:{sourceKey}:{keyword}")
            });

            if (messageId.HasValue)
            {
                // Обновляем существующее сообщение
                await _botClient.EditMessageText(
                    chatId: userId,
                    messageId: messageId.Value,
                    text: text,
                    replyMarkup: new InlineKeyboardMarkup(inline),
                    cancellationToken: ct
                );
            }
            else
            {
                // Отправляем новое сообщение
                await _botClient.SendMessage(
                    chatId: userId,
                    text: text,
                    replyMarkup: new InlineKeyboardMarkup(inline),
                    cancellationToken: ct
                );
            }
        }

        private async Task HandleCallback(CallbackQuery query, CancellationToken ct)
        {
            var chatId = query.Message!.Chat.Id;
            var messageId = query.Message.MessageId;

            try
            {
                if (query.Data is null)
                    return;

                if (query.Data.StartsWith("page:"))
                {
                    var parts = query.Data.Split(':');
                    if (parts.Length == 4 && int.TryParse(parts[3], out var page))
                    {
                        var sourceKey = parts[1];
                        var keyword = parts[2];
                        using var scope = _serviceProvider.CreateScope();
                        var discountService = scope.ServiceProvider.GetRequiredService<DiscountService>();
                        var items = (await discountService.GetOrCollectAsync(keyword, TimeSpan.FromHours(1), ct)).ToList();
                        await SendPagedResults(chatId, sourceKey, keyword, items, page, ct, messageId);
                        await _botClient.AnswerCallbackQuery(query.Id, cancellationToken: ct);
                    }
                }
                else if (query.Data.StartsWith("sub:"))
                {
                    var parts = query.Data.Split(':');
                    if (parts.Length < 3) return;
                    var sourceKey = parts[1];
                    var keyword = parts[2];
                    using var scope = _serviceProvider.CreateScope();
                    var apiRepo = scope.ServiceProvider.GetRequiredService<IApiSubscriptionRepository>();
                    var userApiRepo = scope.ServiceProvider.GetRequiredService<IUserSubscriptionRepository>();
                    var api = await apiRepo.GetOrCreateAsync(sourceKey, keyword, ct);
                    var us = await userApiRepo.GetAsync(chatId, api.Id, ct);
                    if (us is null || !us.Subscribed)
                    {
                        await userApiRepo.UpsertAsync(new UserSubscription
                        {
                            Id = us?.Id ?? Guid.NewGuid(),
                            UserId = chatId,
                            ApiSubscriptionId = api.Id,
                            Subscribed = true,
                            UpdatedAtUtc = DateTime.UtcNow
                        }, ct);
                        await _botClient.AnswerCallbackQuery(query.Id, "Подписка добавлена", cancellationToken: ct);
                    }
                    else
                    {
                        await _botClient.AnswerCallbackQuery(query.Id, "Вы уже подписаны", cancellationToken: ct);
                    }
                }
                else if (query.Data.StartsWith("unsub:"))
                {
                    var parts = query.Data.Split(':');
                    if (parts.Length < 3) return;
                    var sourceKey = parts[1];
                    var keyword = parts[2];
                    using var scope = _serviceProvider.CreateScope();
                    var apiRepo = scope.ServiceProvider.GetRequiredService<IApiSubscriptionRepository>();
                    var userApiRepo = scope.ServiceProvider.GetRequiredService<IUserSubscriptionRepository>();
                    var api = await apiRepo.GetOrCreateAsync(sourceKey, keyword, ct);
                    var us = await userApiRepo.GetAsync(chatId, api.Id, ct);
                    if (us is not null && us.Subscribed)
                    {
                        us.Subscribed = false;
                        us.UpdatedAtUtc = DateTime.UtcNow;
                        await userApiRepo.UpsertAsync(us, ct);
                    }
                    await _botClient.AnswerCallbackQuery(query.Id, "Подписка удалена", cancellationToken: ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Callback handling error");
                await _botClient.AnswerCallbackQuery(query.Id, "Ошибка при обработке", cancellationToken: ct);
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