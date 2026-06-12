using Microsoft.Extensions.Logging;
using NSubstitute;
using OrderProcessor.Application.Contracts.Pricing;
using OrderProcessor.Application.Contracts.Repositories;
using OrderProcessor.Application.Contracts.Services;
using OrderProcessor.Application.DTOs;
using OrderProcessor.Application.Services;
using OrderProcessor.Domain.Enums;
using OrderProcessor.Domain.Models;

namespace OrderProcessor.Application.Tests
{
    public class OrderProcessorTests
    {
        [Fact]
        public async Task ProcessAsync_GoldSchoolWithShortEmbroidery_ShouldApplyDiscountAndCharge()
        {
            // Arrange

            var schoolRepository =
                Substitute.For<ISchoolRepository>();

            schoolRepository
                .GetTierAsync(
                    1,
                    Arg.Any<CancellationToken>())
                .Returns(SchoolTier.Gold);

            var productRepository =
                Substitute.For<IProductRepository>();

            productRepository
                .GetBasePriceAsync(
                    "SKU1",
                    Arg.Any<CancellationToken>())
                .Returns(100m);

            var inventoryRepository =
                Substitute.For<IInventoryRepository>();

            inventoryRepository
                .GetStockAsync(
                    "SKU1",
                    Arg.Any<CancellationToken>())
                .Returns(10);

            var paymentClient =
                Substitute.For<IPaymentClient>();

            paymentClient
                .CreateIntentAsync(
                    Arg.Any<decimal>(),
                    Arg.Any<string>(),
                    Arg.Any<CancellationToken>())
                .Returns(true);

            var emailService =
                Substitute.For<IEmailServices>();

            var logger =
                Substitute.For<ILogger<OrderProcessorService>>();

            IPricingService pricingService =
                new PricingService();

            var sut = new OrderProcessorService(
                schoolRepository,
                productRepository,
                inventoryRepository,
                pricingService,
                paymentClient,
                emailService,
                logger);

            var request =
                new ProcessOrderRequest(
                    SchoolId: 1,
                    ParentEmail: "parent@test.com",
                    Lines:
                    [
                        new OrderLine(
                        Sku: "SKU1",
                        Quantity: 1,
                        Embroidery: "ABC")
                    ]);

            // Act

            var result =
                await sut.ProcessAsync(request);

            // Assert

            Assert.True(result.Success);

            Assert.Equal("OK", result.Message);

            Assert.Equal(89.50m, result.Total);

            await paymentClient
                .Received(1)
                .CreateIntentAsync(
                    89.50m,
                    "parent@test.com",
                    Arg.Any<CancellationToken>());

            await emailService
                .Received(1)
                .SendConfirmationAsync(
                    "parent@test.com",
                    89.50m,
                    Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ProcessAsync_OutOfStock_ShouldFail()
        {
            var schoolRepository =
                Substitute.For<ISchoolRepository>();

            schoolRepository
                .GetTierAsync(
                    1,
                    Arg.Any<CancellationToken>())
                .Returns(SchoolTier.Gold);

            var productRepository =
                Substitute.For<IProductRepository>();

            productRepository
                .GetBasePriceAsync(
                    "SKU1",
                    Arg.Any<CancellationToken>())
                .Returns(100m);

            var inventoryRepository =
                Substitute.For<IInventoryRepository>();

            inventoryRepository
                .GetStockAsync(
                    "SKU1",
                    Arg.Any<CancellationToken>())
                .Returns(0);

            var sut = new OrderProcessorService(
                schoolRepository,
                productRepository,
                inventoryRepository,
                new PricingService(),
                Substitute.For<IPaymentClient>(),
                Substitute.For<IEmailServices>(),
                Substitute.For<ILogger<OrderProcessorService>>());

            var result =
                await sut.ProcessAsync(
                    new ProcessOrderRequest(
                        1,
                        "parent@test.com",
                        [
                            new OrderLine(
                        "SKU1",
                        5,
                        null)
                        ]));

            Assert.False(result.Success);

            Assert.Equal(
                "Out of stock: SKU1",
                result.Message);
        }
    }
}