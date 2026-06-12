using Microsoft.Extensions.Logging;
using OrderProcessor.Application.Contracts.Email;
using OrderProcessor.Application.Contracts.Services;

namespace OrderProcessor.Infrastructure.Notifications
{
    public sealed class EmailServices : IEmailServices
    {
        private readonly IEmailSender _emailSender;
        private readonly ILogger<EmailServices> _logger;
        public EmailServices(IEmailSender emailSender,  ILogger<EmailServices> logger)
        {
            _emailSender = emailSender;
            _logger = logger;
        }
        public async Task SendConfirmationAsync(
            string email,
            decimal total,
            CancellationToken ct)
        {
            _logger.LogInformation(
                  "Sending order confirmation email to {Email} with Total: {Total}",
                  email,
                  total);

            var subject = "Order Confirmation";

            var body = $"""
            <h2>Thank you for your order!</h2>
            <p>Your order has been successfully received.</p>
            <p>Total Amount is £: <strong>{total:C}</strong></p>
            """;

            try
            {
                await _emailSender.SendAsync(
                    email,
                    subject,
                    body,
                    ct);

                _logger.LogInformation(
                    "Order confirmation email sent successfully to {Email}",
                    email);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send order confirmation email to {Email}",
                    email);

                throw new Exception("An Error Occured while sending an email", ex);
            }
        }
    }
}