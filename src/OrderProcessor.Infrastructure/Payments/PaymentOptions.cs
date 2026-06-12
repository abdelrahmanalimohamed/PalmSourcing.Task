namespace OrderProcessor.Infrastructure.Payments
{
    public sealed class PaymentOptions
    {
        public const string SectionName = "Payments";
        public string BaseUrl { get; init; } = string.Empty;
    }
}