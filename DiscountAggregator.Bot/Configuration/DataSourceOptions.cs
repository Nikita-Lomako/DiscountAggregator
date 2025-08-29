namespace DiscountAggregator.Bot.Configuration
{
    public class DataSourceOptions
    {
        public const string SectionName = "DataSources";
        
        public string JsonFilePath { get; set; } = "discounts.json";
    }
} 