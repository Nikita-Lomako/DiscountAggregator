namespace DiscountAggregator.Bot.Configuration
{
    public class DataSourceOptions
    {
        public const string SectionName = "DataSources";
        
        public string JsonFilePath { get; set; } = "discounts.json";
        public int CollectorIntervalSeconds { get; set; } = 300;
        public string[] DefaultKeywords { get; set; } = new[] { "ноутбук" };
        public string? ProxyUrl { get; set; }
    }
} 