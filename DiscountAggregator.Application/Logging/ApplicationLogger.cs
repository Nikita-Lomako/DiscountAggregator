using Microsoft.Extensions.Logging;

namespace DiscountAggregator.Application.Logging
{
    public static class ApplicationLogger
    {
        public static class Categories
        {
            public const string Service = "Application.Service";
            public const string Command = "Application.Command";
            public const string Query = "Application.Query";
            public const string Validation = "Application.Validation";
        }
    }
} 