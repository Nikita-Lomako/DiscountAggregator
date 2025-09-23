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
            var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var userCategorySubscriptionRepo = scope.ServiceProvider.GetRequiredService<IUserCategorySubscriptionRepository>();
            var searchQueryRepo = scope.ServiceProvider.GetRequiredService<ISearchQueryRepository>();
            var notifier = scope.ServiceProvider.GetRequiredService<INotificationService>();

            try
            {
                if (text.StartsWith("/start"))
                {
                    // Создаем или получаем пользователя
                    await GetOrCreateUserAsync(userId, message, userRepo, cancellationToken);

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
                    var subscriptions = await userCategorySubscriptionRepo.GetActiveByUserIdAsync(userId, cancellationToken);
                    if (!subscriptions.Any())
                    {
                        await botClient.SendMessage(userId, "У вас нет подписок. Введите /search <ключевое_слово> и подпишитесь из сообщения.", cancellationToken: cancellationToken);
                    }
                    else
                    {
                        var lines = subscriptions
                            .Select(s => $"- {s.SourceFilter}: {s.Keyword}")
                            .ToList();
                        await botClient.SendMessage(userId, "Ваши активные подписки на категории:\n" + string.Join("\n", lines), cancellationToken: cancellationToken);
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

                    var products = await discountService.GetOrCollectAsync(keyword, TimeSpan.FromHours(1), cancellationToken);

                    if (!products.Any())
                    {
                        await botClient.SendMessage(
                            chatId: userId,
                            text: "Скидки не найдены.",
                            cancellationToken: cancellationToken
                        );
                        return;
                    }

                    // Создаем или получаем пользователя
                    await GetOrCreateUserAsync(userId, message, userRepo, cancellationToken);

                    await searchQueryRepo.AddQueryAsync(new SearchQuery 
                    { 
                        UserId = userId, 
                        Keyword = keyword, 
                        SourceFilter = "wildberries",
                        KeywordNormalized = keyword.ToLowerInvariant(),
                        QueriedAtUtc = DateTime.UtcNow
                    }, cancellationToken);
                    
                    var page = 1;
                    await SendPagedResults(userId, "wildberries", keyword, products.ToList(), page, cancellationToken);
                }
                else if (text.StartsWith("/recent"))
                {
                    var queries = await searchQueryRepo.GetRecentByUserIdAsync(userId, 1, cancellationToken);
                    var keywords = queries.Select(q => q.Keyword).Distinct().Take(10).ToList();
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

        private async Task SendPagedResults(long userId, string sourceKey, string keyword, List<Domain.Entities.Product> items, int page, CancellationToken ct, int? messageId = null)
        {
            const int pageSize = 10;
            var totalPages = Math.Max(1, (int)Math.Ceiling(items.Count / (double)pageSize));
            page = Math.Clamp(page, 1, totalPages);

            var slice = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            var text = string.Join("\n\n", slice.Select(p => $"{p.Title}\nБренд: {p.Brand}\nЦена: {p.CurrentPrice} (было {p.OldPrice})\nСкидка: {p.DiscountPercent}%\nСсылка: {p.Url}"));

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

        private async Task<Domain.Entities.User> GetOrCreateUserAsync(long userId, Message message, IUserRepository userRepo, CancellationToken cancellationToken)
        {
            var user = await userRepo.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                user = new Domain.Entities.User
                {
                    Id = userId,
                    Username = message.From?.Username,
                    RegisteredAtUtc = DateTime.UtcNow,
                    LastActivityAtUtc = DateTime.UtcNow
                };
                await userRepo.UpsertAsync(user, cancellationToken);
            }
            else
            {
                // Обновляем время последней активности
                user.LastActivityAtUtc = DateTime.UtcNow;
                await userRepo.UpsertAsync(user, cancellationToken);
            }
            return user;
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
                    var discountService = scope.ServiceProvider.GetRequiredService<DiscountService>();
                    var userCategorySubscriptionRepo = scope.ServiceProvider.GetRequiredService<IUserCategorySubscriptionRepository>();
                    
                    // Создаем подписку на категорию
                    var subscription = new Domain.Entities.UserCategorySubscription
                    {
                        UserId = chatId,
                        Keyword = keyword,
                        SourceFilter = sourceKey,
                        IsActive = true,
                        SubscribedAtUtc = DateTime.UtcNow
                    };
                    
                    await userCategorySubscriptionRepo.UpsertAsync(subscription, ct);
                    
                    // Сохраняем товары из кеша в базу данных
                    await discountService.SaveProductsToDatabaseAsync(keyword, ct);
                    
                    await _botClient.AnswerCallbackQuery(query.Id, $"Подписка на '{keyword}' добавлена", cancellationToken: ct);
                }
                else if (query.Data.StartsWith("unsub:"))
                {
                    var parts = query.Data.Split(':');
                    if (parts.Length < 3) return;
                    var sourceKey = parts[1];
                    var keyword = parts[2];
                    using var scope = _serviceProvider.CreateScope();
                    var userCategorySubscriptionRepo = scope.ServiceProvider.GetRequiredService<IUserCategorySubscriptionRepository>();
                    var discountService = scope.ServiceProvider.GetRequiredService<DiscountService>();
                    
                    // Отписываемся от категории
                    await userCategorySubscriptionRepo.DeleteAsync(chatId, keyword, sourceKey, ct);
                    
                    // Удаляем все данные связанные с этой категорией
                    var deletedCount = await discountService.DeleteProductsByKeywordAsync(keyword, ct);
                    
                    await _botClient.AnswerCallbackQuery(query.Id, 
                        $"Подписка на '{keyword}' удалена. Удалено товаров: {deletedCount}", 
                        cancellationToken: ct);
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