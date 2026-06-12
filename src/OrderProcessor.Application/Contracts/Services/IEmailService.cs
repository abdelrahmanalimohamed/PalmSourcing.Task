namespace OrderProcessor.Application.Contracts.Services
{
    public interface IEmailService
    {
        Task SendConfirmationAsync(
               string email,
               decimal total,
               CancellationToken ct);
    }
}