using OrderProcessor.Application.Contracts.Pricing;
using OrderProcessor.Domain.Enums;

namespace OrderProcessor.Application.Services
{
    public sealed class PricingService : IPricingService
    {
        public decimal CalculatePrice(
            decimal basePrice, 
            SchoolTier tier, 
            string? embroidery)
        {
            var price = tier switch
            {
                SchoolTier.Gold => basePrice * 0.85m,
                SchoolTier.Silver => basePrice * 0.92m,
                _ => basePrice
            };

            if (!string.IsNullOrWhiteSpace(embroidery))
            {
                price += embroidery.Length <= 3
                    ? 4.50m
                    : 8.00m;
            }

            return price;
        }
    }
}