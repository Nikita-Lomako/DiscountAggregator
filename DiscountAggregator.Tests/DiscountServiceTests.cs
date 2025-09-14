using Moq;
using Xunit;
using DiscountAggregator.Application.Services;
using DiscountAggregator.Domain.Interfaces;
using DiscountAggregator.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using DiscountAggregator.Application.Interfaces;
using DiscountAggregator.Application.DTOs;

namespace DiscountAggregator.Tests
{
    public class DiscountServiceTests
    {
        private readonly Mock<IDiscountSource> _discountSourceMock;
        private readonly Mock<IDiscountRepository> _discountRepositoryMock;
        private readonly DiscountService _discountService;

        public DiscountServiceTests()
        {
            _discountSourceMock = new Mock<IDiscountSource>();
            _discountRepositoryMock = new Mock<IDiscountRepository>();
            _discountService = new DiscountService(_discountSourceMock.Object, _discountRepositoryMock.Object);
        }

        [Fact]
        public async Task CollectDiscountsAsync_ShouldFetchAndSaveDiscounts()
        {
            // Arrange
            var keyword = "test";
            var rawDiscounts = new List<RawDiscountDto>
            {
                new RawDiscountDto { ExternalId = "1", Title = "Test 1", Price = 10, OldPrice = 20, Url = "http://test1.com" },
                new RawDiscountDto { ExternalId = "2", Title = "Test 2", Price = 15, OldPrice = 30, Url = "http://test2.com" }
            };

            _discountSourceMock.Setup(s => s.FetchAsync(It.IsAny<SourceFetchRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(rawDiscounts);

            _discountSourceMock.Setup(s => s.SourceKey).Returns("test-source");

            // Act
            var result = await _discountService.CollectDiscountsAsync(keyword);

            // Assert
            Assert.Equal(2, result);
            _discountRepositoryMock.Verify(r => r.UpsertAsync(It.IsAny<Discount>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }
    }
}
