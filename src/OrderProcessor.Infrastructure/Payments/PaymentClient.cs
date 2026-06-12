using OrderProcessor.Application.Contracts.Services;

namespace OrderProcessor.Infrastructure.Payments
{
    public sealed class PaymentClient : IPaymentClient
    {
        private readonly HttpClient _httpClient;
        private readonly PaymentOptions _options;
        public PaymentClient(
            HttpClient httpClient, PaymentOptions options)
        {
            _httpClient = httpClient;
            _options = options;
        }
        public async Task<bool> CreateIntentAsync(
            decimal amount,
            string email,
            CancellationToken ct)
        {
            var content =
                new FormUrlEncodedContent(
                [
                    new("amount", amount.ToString()),
                    new("email", email)
                ]);

            var response =
                await _httpClient.PostAsync(
                    _options.BaseUrl,
                    content,
                    ct);

            return response.IsSuccessStatusCode;
        }
    }
}