using DiscountAggregator.Application.DTOs;
using DiscountAggregator.Application.Interfaces;
using DiscountAggregator.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace DiscountAggregator.Application.Sources.Wildberries
{
    public class WildberriesSourcePlaywright : IDiscountSource
    {
        private readonly ILogger<WildberriesSourcePlaywright> _logger;

        public WildberriesSourcePlaywright(ILogger<WildberriesSourcePlaywright> logger)
        {
            _logger = logger;
        }

        public string SourceKey => "wildberries";

        public async Task<IEnumerable<Product>> FetchAsync(SourceFetchRequest request, CancellationToken ct = default)
        {
            var keyword = string.IsNullOrWhiteSpace(request.Keyword) ? "скидки" : request.Keyword.Trim();
            var searchUrl = $"https://www.wildberries.ru/catalog/0/search.aspx?page=1&sort=popular&search={Uri.EscapeDataString(keyword)}";

            var items = new List<Product>();

            try
            {
                using var playwright = await Playwright.CreateAsync();
                await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = true,
                    Args = new[] {
                        "--disable-blink-features=AutomationControlled",
                        "--no-sandbox",
                        "--disable-setuid-sandbox",
                        "--disable-dev-shm-usage",
                        "--disable-gpu",
                        "--disable-web-security",
                        "--disable-features=VizDisplayCompositor",
                        "--single-process",
                        "--no-zygote"
                    }
                });

                var context = await browser.NewContextAsync(new BrowserNewContextOptions
                {
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124 Safari/537.36",
                    ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
                });

                var page = await context.NewPageAsync();

                _logger.LogInformation("Переход по адресу WB: {Url}", searchUrl);
                try
                {
                    await page.GotoAsync(searchUrl, new PageGotoOptions { Timeout = 60000 });
                }
                catch (TimeoutException)
                {
                    _logger.LogWarning("Таймаут при загрузке страницы WB: {Url}", searchUrl);
                    return Enumerable.Empty<Product>();
                }
                // Ждем появления карточек товаров
                try
                {
                    await page.WaitForSelectorAsync("article.product-card", new PageWaitForSelectorOptions
                    {
                        Timeout = 30000,
                        State = WaitForSelectorState.Attached
                    });
                }
                catch (TimeoutException)
                {
                    _logger.LogWarning("Карточки товаров не найдены на WB по запросу: {Keyword}", keyword);
                    return Enumerable.Empty<Product>();
                }

                var cards = await page.QuerySelectorAllAsync("article.product-card");
                _logger.LogInformation("WB url: {Url}; найдено карточек: {Count}", searchUrl, cards.Count);

                if (!cards.Any())
                {
                    _logger.LogWarning("WB: карточки не найдены по запросу {Keyword}", keyword);
                    return Enumerable.Empty<Product>();
                }

                foreach (var card in cards.Take(Math.Max(1, request.Limit)))
                {
                    if (ct.IsCancellationRequested)
                        break;
                    try
                    {
                        var idAttr = await card.GetAttributeAsync("data-nm-id");
                    var linkNode = await card.QuerySelectorAsync("a.product-card__link");
                    var brandNode = await card.QuerySelectorAsync("span.product-card__brand");
                    var nameNode = await card.QuerySelectorAsync("span.product-card__name");
                    var priceNode = await card.QuerySelectorAsync("ins.price__lower-price, span.price__lower-price");
                    var oldNode = await card.QuerySelectorAsync("del");

                    var urlNode = linkNode is not null ? await linkNode.GetAttributeAsync("href") : string.Empty;
                    string urlAbs = !string.IsNullOrWhiteSpace(urlNode) && urlNode.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                        ? urlNode
                        : $"https://www.wildberries.ru{urlNode}";

                    decimal ParsePrice(string? text)
                    {
                        if (string.IsNullOrWhiteSpace(text)) return 0m;
                        var digits = new string(text.Where(char.IsDigit).ToArray());
                        return decimal.TryParse(digits, out var val) ? val : 0m;
                    }

                    var title = linkNode is not null
                        ? (await linkNode.GetAttributeAsync("aria-label")) ?? string.Empty
                        : (await nameNode?.InnerTextAsync())?.Trim() ?? string.Empty;

                    var brand = await (brandNode?.InnerTextAsync() ?? Task.FromResult(string.Empty));
                    var price = ParsePrice(await (priceNode?.InnerTextAsync() ?? Task.FromResult(string.Empty)));
                    var oldPrice = ParsePrice(await (oldNode?.InnerTextAsync() ?? Task.FromResult(string.Empty)));

                    items.Add(new Product
                    {
                        Id = Guid.NewGuid(),
                        Source = SourceKey,
                        ExternalId = string.IsNullOrWhiteSpace(idAttr) ? Guid.NewGuid().ToString() : idAttr,
                        Title = title,
                        Brand = brand,
                        CurrentPrice = price,
                        OldPrice = oldPrice > 0 ? oldPrice : price,
                        Url = urlAbs,
                        LastUpdatedAtUtc = DateTime.UtcNow
                    });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Ошибка при обработке карточки товара на WB");
                        continue;
                    }
                }

                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WB fetch error for keyword {Keyword}", keyword);
                return Enumerable.Empty<Product>();
            }
        }
    }
}

