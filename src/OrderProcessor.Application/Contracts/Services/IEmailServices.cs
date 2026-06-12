namespace OrderProcessor.Application.Contracts.Services
{
    public interface IEmailServices
    {
        Task SendConfirmationAsync(
               string email,
               decimal total,
               CancellationToken ct);
    }
}