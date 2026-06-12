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
    public class OrderProcessorService : IOrderProcessor
    {
        // Semaphore caps concurrent per-line DB/service calls under peak load.
        // 10 means at most 10 lines are fetched in parallel at any moment.
        private const int MaxConcurrentLines = 10;

        private readonly ISchoolRepository _schools;
        private readonly IProductRepository _products;
        private readonly IInventoryRepository _inventory;
        private readonly IPricingService _pricing;
        private readonly IPaymentClient _payments;
        private readonly IEmailServices _email;
        private readonly ILogger<OrderProcessorService> _logger;

        public OrderProcessorService(
            ISchoolRepository schools,
            IProductRepository products,
            IInventoryRepository inventory,
            IPricingService pricing,
            IPaymentClient payments,
            IEmailServices email,
            ILogger<OrderProcessorService> logger)
        {
            _schools = schools;
            _products = products;
            _inventory = inventory;
            _pricing = pricing;
            _payments = payments;
            _email = email;
            _logger = logger;
        }

        public async Task<OrderResult> ProcessAsync(
            ProcessOrderRequest request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.Lines.Count == 0)
                return OrderResult.Fail("Order contains no items.");

            SchoolTier? tier;
            try
            {
                tier = await _schools.GetTierAsync(request.SchoolId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch tier for school {SchoolId}", request.SchoolId);
                return OrderResult.Fail("Unable to verify school. Please try again.");
            }

            if (tier is null)
                return OrderResult.Fail("School not found.");

            ProcessedLine[] processedLines;
            try
            {
                using var throttle = new SemaphoreSlim(MaxConcurrentLines);
                processedLines = await Task.WhenAll(
                    request.Lines.Select(line =>
                        ProcessLineAsync(line, tier.Value, throttle, ct)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process order lines for school {SchoolId}", request.SchoolId);
                return OrderResult.Fail("Unable to verify stock or pricing. Please try again.");
            }

            var outOfStock = Array.Find(processedLines, l => !l.InStock);

            if (outOfStock is not null)
                return OrderResult.Fail($"Out of stock: {outOfStock.Sku}");

            var subtotal = processedLines.Sum(l => l.LineTotal);

            bool paymentSucceeded;
            try
            {
                paymentSucceeded = await _payments.CreateIntentAsync(
                    subtotal, request.ParentEmail, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment service threw for {Email}", request.ParentEmail);
                return OrderResult.Fail("Payment service unavailable. Please try again.");
            }

            if (!paymentSucceeded)
                return OrderResult.Fail("Payment failed.");
            try
            {
                await _email.SendConfirmationAsync(request.ParentEmail, subtotal, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Confirmation email failed for {Email}. Order subtotal was {Subtotal}",
                    request.ParentEmail, subtotal);
            }

            return OrderResult.Ok(subtotal);
        }
        private async Task<ProcessedLine> ProcessLineAsync(
            OrderLine line,
            SchoolTier tier,
            SemaphoreSlim throttle,
            CancellationToken ct)
        {
            await throttle.WaitAsync(ct);
            try
            {
                var stockTask = _inventory.GetStockAsync(line.Sku, ct);
                var priceTask = _products.GetBasePriceAsync(line.Sku, ct);
                await Task.WhenAll(stockTask, priceTask);

                var stock = stockTask.Result;
                var basePrice = priceTask.Result;

                if (stock < line.Quantity)
                    return new ProcessedLine(line.Sku, InStock: false, LineTotal: 0);

                var unitPrice = _pricing.CalculatePrice(basePrice, tier, line.Embroidery);
                return new ProcessedLine(line.Sku, InStock: true, LineTotal: unitPrice * line.Quantity);
            }
            finally
            {
                throttle.Release();
            }
        }
    }
}