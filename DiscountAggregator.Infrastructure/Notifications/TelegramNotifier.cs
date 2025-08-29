using DiscountAggregator.Domain.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DiscountAggregator.Infrastructure.Notifications
{
    public class TelegramNotifier : INotificationService
    {
        private readonly ITelegramBotClient _botClient;

        public TelegramNotifier(ITelegramBotClient botClient)
        {
            _botClient = botClient ?? throw new ArgumentNullException(nameof(botClient));
        }

        public async Task NotifyAsync(long userId, string message, CancellationToken ct = default)
        {
            await _botClient.SendMessage(
                chatId: new ChatId(userId),
                text: message,
                cancellationToken: ct
            );
        }
    }
}
