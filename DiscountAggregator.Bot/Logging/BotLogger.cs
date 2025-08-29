using Microsoft.Extensions.Logging;

namespace DiscountAggregator.Bot.Logging
{
    public static class BotLogger
    {
        public static class Categories
        {
            public const string Bot = "Bot.Telegram";
            public const string Handler = "Bot.Handler";
            public const string Command = "Bot.Command";
            public const string HostedService = "Bot.HostedService";
        }
    }
} 