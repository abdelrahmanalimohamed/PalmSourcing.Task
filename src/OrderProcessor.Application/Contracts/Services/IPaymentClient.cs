namespace OrderProcessor.Application.Contracts.Services
{
    public interface IPaymentClient
    {
        Task<bool> CreateIntentAsync(
               decimal amount,
               string email,
               CancellationToken ct);
    }
}