using DiscountAggregator.Domain.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DiscountAggregator.Infrastructure.Notifications
{
    public class TelegramNotifier : INotificationService
    {
        private readonly TelegramBotClient _botClient;

        public TelegramNotifier(string botToken)
        {
            _botClient = new TelegramBotClient(botToken);
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
