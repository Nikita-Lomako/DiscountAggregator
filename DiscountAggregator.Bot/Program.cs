using DiscountAggregator.Application.Services;
using DiscountAggregator.Infrastructure.Notifications;
using DiscountAggregator.Infrastructure.Persistence;
using DiscountAggregator.Infrastructure.Sources.Wildberries;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

class Program
{
    private static TelegramBotClient _botClient = null!;
    private static DiscountService _service = null!;
    private static JsonDiscountRepository _repository = null!;
    private static TelegramNotifier _notifier = null!;

    static async Task Main(string[] args)
    {
        string botToken = "8295591284:AAFacmgdQuacyxrVnZAaorOQHk51bRpUd5Q";
        string jsonPath = "discounts.json";

        var source = new WildberriesSource();
        _repository = new JsonDiscountRepository(jsonPath);
        _service = new DiscountService(source, _repository);
        _notifier = new TelegramNotifier(botToken);

        _botClient = new TelegramBotClient(botToken);

        Console.WriteLine("Бот запущен. Ожидание команд...");

        using var cts = new CancellationTokenSource();

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        _botClient.StartReceiving(
     HandleUpdateAsync,
     HandleErrorAsync,
     receiverOptions,
     cts.Token
 );


        Console.ReadLine();
        cts.Cancel();
    }

    private static async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        if (update.Type != UpdateType.Message || update.Message?.Text is null)
            return;

        var message = update.Message;
        var userId = message.Chat.Id;
        var text = message.Text;

        if (text.StartsWith("/start"))
        {
            await bot.SendMessage(
                chatId: userId,
                text: "Добро пожаловать! Используйте /search <ключевое_слово> для поиска скидок.",
                cancellationToken: ct
            );
        }
        else if (text.StartsWith("/search "))
        {
            var keyword = text.Substring(8).Trim();
            if (string.IsNullOrWhiteSpace(keyword))
            {
                await bot.SendMessage(
                    chatId: userId,
                    text: "Пожалуйста, укажите ключевое слово: /search ноутбук",
                    cancellationToken: ct
                );
                return;
            }

            await bot.SendMessage(
                chatId: userId,
                text: $"Ищу скидки по запросу: {keyword}...",
                cancellationToken: ct
            );

            int count = await _service.CollectDiscountsAsync(keyword, ct);
            var discounts = await _repository.SearchAsync(keyword, ct);

            if (!discounts.Any())
            {
                await bot.SendMessage(
                    chatId: userId,
                    text: "Скидки не найдены.",
                    cancellationToken: ct
                );
                return;
            }

            foreach (var discount in discounts)
            {
                string msg = $"{discount.Title}\nБренд: {discount.Brand}\nЦена: {discount.Price} (было {discount.OldPrice})\nСсылка: {discount.Url}";
                await _notifier.NotifyAsync(userId, msg, ct);
            }
        }
        else
        {
            await bot.SendMessage(
                chatId: userId,
                text: "Неизвестная команда. Используйте /search <ключевое_слово>.",
                cancellationToken: ct
            );
        }
    }

    private static Task HandleErrorAsync(
        ITelegramBotClient bot,
        Exception exception,
        HandleErrorSource source,
        CancellationToken ct)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiEx => $"Ошибка Telegram API:\n[{apiEx.ErrorCode}] {apiEx.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(errorMessage);
        return Task.CompletedTask;
    }
}
