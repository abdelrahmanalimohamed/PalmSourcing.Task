using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using OrderProcessor.Application.Contracts.Email;

namespace OrderProcessor.Infrastructure.Notifications
{
    public sealed class SmtpEmailSender : IEmailSender
    {
        private readonly EmailOptions _settings;
        private readonly ILogger<SmtpEmailSender> _logger;
        public SmtpEmailSender(
            IOptions<EmailOptions> settings , 
            ILogger<SmtpEmailSender> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }
        public async Task SendAsync(
            string to,
            string subject,
            string htmlBody,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                     "Sending email to {Recipient}. Subject: {Subject}",
                     to,
                     subject);

                var message = new MimeMessage();

                message.From.Add(
                    new MailboxAddress(
                        _settings.DisplayName,
                        _settings.Mail));

                message.To.Add(new MailboxAddress("", to));
                message.Subject = subject;

                message.Body = new BodyBuilder
                {
                    HtmlBody = htmlBody
                }.ToMessageBody();

                using var client = new SmtpClient();

                await client.ConnectAsync(
                    _settings.Host,
                    _settings.Port,
                    false,
                    cancellationToken);

                await client.AuthenticateAsync(
                    _settings.Mail,
                    _settings.Password,
                    cancellationToken);

                await client.SendAsync(message, cancellationToken);

                await client.DisconnectAsync(true, cancellationToken);

                _logger.LogInformation(
                  "Email sent successfully to {Recipient}",
                  to);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send email to {Recipient}. Subject: {Subject}",
                    to,
                    subject);

                throw new Exception("An Error Occured while sending an email", ex);
            }

        }
    }
}
