using OrderProcessor.Domain.Enums;

namespace OrderProcessor.Application.Contracts.Pricing
{
    public interface IPricingService
    {
        decimal CalculatePrice(
            decimal basePrice,
            SchoolTier tier,
            string? embroidery);
    }
}