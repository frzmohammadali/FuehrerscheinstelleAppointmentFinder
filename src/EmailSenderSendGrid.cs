using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace FuehrerscheinstelleAppointmentFinder
{
    internal class EmailSenderSendGrid : IEmailSender
    {

        private readonly ILogger<EmailSenderSendGrid> _logger;
        private readonly SendGridOptions _sendGridOptions;

        public EmailSenderSendGrid(IOptions<AppOptions> optionsAccessor,
                           ILogger<EmailSenderSendGrid> logger)
        {
            Options = optionsAccessor.Value;
            _sendGridOptions = Options.EmailSenderOptions.SendGridOptions;
            _logger = logger;
        }

        public AppOptions Options { get; } //Set with Secret Manager.

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            if (string.IsNullOrEmpty(_sendGridOptions.ApiKey))
            {
                throw new Exception("Null SendGridKey");
            }
            await Execute(_sendGridOptions.ApiKey, subject, message, toEmail);
        }

        private async Task Execute(string apiKey, string subject, string message, string toEmail)
        {
            var client = new SendGridClient(apiKey);
            var msg = new SendGridMessage()
            {
                From = new EmailAddress(_sendGridOptions.SenderEmail,
                    _sendGridOptions.SenderName),
                Subject = subject,
                PlainTextContent = message,
                HtmlContent = message
            };
            msg.AddTo(new EmailAddress(toEmail));

            // Disable click tracking.
            // See https://sendgrid.com/docs/User_Guide/Settings/tracking.html
            msg.SetClickTracking(false, false);
            var response = await client.SendEmailAsync(msg);
            _logger.LogInformation(response.IsSuccessStatusCode
                                   ? $"Email to {toEmail} queued successfully!"
                                   : $"Failure Email to {toEmail}");
        }
    }
}
