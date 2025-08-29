using Microsoft.Extensions.Logging;

namespace DiscountAggregator.Infrastructure.Logging
{
    public static class InfrastructureLogger
    {
        public static class Categories
        {
            public const string Repository = "Infrastructure.Repository";
            public const string Source = "Infrastructure.Source";
            public const string Notification = "Infrastructure.Notification";
            public const string HttpClient = "Infrastructure.HttpClient";
        }
    }
} 