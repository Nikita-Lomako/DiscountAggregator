namespace DiscountAggregator.Bot.Configuration
{
    public class TelegramBotOptions
    {
        public const string SectionName = "TelegramBot";
        
        public string Token { get; set; } = string.Empty;
        public string[] AllowedUpdates { get; set; } = Array.Empty<string>();
    }
} 