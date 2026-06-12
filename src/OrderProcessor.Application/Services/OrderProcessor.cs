using Microsoft.Extensions.Logging;
using OrderProcessor.Application.Contracts.Pricing;
using OrderProcessor.Application.Contracts.Repositories;
using OrderProcessor.Application.Contracts.Services;
using OrderProcessor.Application.DTOs;
using OrderProcessor.Application.Models;
using OrderProcessor.Domain.Enums;
using OrderProcessor.Domain.Models;

namespace OrderProcessor.Application.Services
{
    public sealed class OrderProcessor : IOrderProcessor
    {
        private readonly ISchoolRepository _schools;
        private readonly IProductRepository _products;
        private readonly IInventoryRepository _inventory;

        private readonly IPricingService _pricing;
        private readonly IEmailService _email;

        private readonly IPaymentClient _payments;
        private readonly ILogger<OrderProcessor> _logger;
        public OrderProcessor(
            ISchoolRepository schools,
            IProductRepository products,
            IInventoryRepository inventory,
            IPricingService pricing,
            IPaymentClient payments,
            IEmailService email,
            ILogger<OrderProcessor> logger)
        {
            _schools = schools;
            _products = products;
            _inventory = inventory;
            _pricing = pricing;
            _payments = payments;
            _email = email;
            _logger = logger;
        }
        public async Task<OrderResult> ProcessAsync(ProcessOrderRequest request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.Lines.Count == 0)
            {
                return OrderResult.Fail("Order contains no items.");
            }

            var tier = await _schools.GetTierAsync(
                    request.SchoolId,
                    ct);

            if (tier is null)
            {
                return OrderResult.Fail(
                    "School not found.");
            }

            var processedLines = await Task.WhenAll(
                    request.Lines.Select(
                        x => ProcessLineAsync(
                            x,
                            tier.Value,
                            ct)));

            var outOfStock = processedLines.FirstOrDefault(
                    x => !x.InStock);

            if (outOfStock is not null)
            {
                return OrderResult.Fail(
                    $"Out of stock: {outOfStock.Sku}");
            }

            var subtotal = processedLines.Sum(x => x.LineTotal);

            var paymentSucceeded = await _payments.CreateIntentAsync(
                    subtotal,
                    request.ParentEmail,
                    ct);

            if (!paymentSucceeded)
            {
                return OrderResult.Fail(
                    "Payment failed.");
            }

            try
            {
                await _email.SendConfirmationAsync(
                    request.ParentEmail,
                    subtotal,
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to send confirmation email");
            }

            return OrderResult.Ok(subtotal);

        }
        private async Task<ProcessedLine> ProcessLineAsync(
            OrderLine line,
            SchoolTier tier,
            CancellationToken ct)
        {
            var stockTask = _inventory.GetStockAsync(
                    line.Sku,
                    ct);

            var priceTask = _products.GetBasePriceAsync(
                    line.Sku,
                    ct);

            await Task.WhenAll(
                stockTask,
                priceTask);

            var stock =  await stockTask;

            if (stock < line.Quantity)
            {
                return new ProcessedLine(
                    line.Sku,
                    false,
                    0);
            }

            var unitPrice =
                _pricing.CalculatePrice(
                    await priceTask,
                    tier,
                    line.Embroidery);

            return new ProcessedLine(
                line.Sku,
                true,
                unitPrice * line.Quantity);
        }
    }
}
