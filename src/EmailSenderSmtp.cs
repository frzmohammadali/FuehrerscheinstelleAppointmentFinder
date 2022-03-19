using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace FuehrerscheinstelleAppointmentFinder
{
    internal class EmailSenderSmtp : IEmailSender
    {
        private readonly ILogger<EmailSenderSmtp> _logger;
        private readonly SmtpOptions _smtpOptions;

        public EmailSenderSmtp(IOptions<AppOptions> optionsAccessor,
                           ILogger<EmailSenderSmtp> logger)
        {
            Options = optionsAccessor.Value;
            _smtpOptions = Options.EmailSenderOptions.SmtpOptions;
            _logger = logger;
        }

        public AppOptions Options { get; } //Set with Secret Manager.

        public Task SendEmailAsync(string toEmail, string subject, string message)
        {
            // Create a System.Net.Mail.MailMessage object
            var mailMessage = new MailMessage();

            // Add a recipient
            mailMessage.To.Add(toEmail);

            // Add a message subject
            mailMessage.Subject = subject;

            // Add a message body
            mailMessage.Body = message;
            mailMessage.IsBodyHtml = true;

            // Create a System.Net.Mail.MailAddress object and 
            // set the sender email address and display name.
            mailMessage.From = new MailAddress(_smtpOptions.SenderEmail,
                _smtpOptions.SenderName);

            // Create a System.Net.Mail.SmtpClient object
            // and set the SMTP host and port number
            var smtp = new SmtpClient(_smtpOptions.Host,
                _smtpOptions.Port);

            // If your server requires authentication add the below code
            // =========================================================
            // Enable Secure Socket Layer (SSL) for connection encryption
            smtp.EnableSsl = _smtpOptions.EnableSsl;

            // Do not send the DefaultCredentials with requests
            smtp.UseDefaultCredentials = false;

            // Create a System.Net.NetworkCredential object and set
            // the username and password required by your SMTP account
            smtp.Credentials = new NetworkCredential(_smtpOptions.SenderEmail, _smtpOptions.Password);
            // =========================================================

            // Send the message
            try
            {
                _ = smtp.SendMailAsync(mailMessage);
                _logger.LogInformation("Email to {toEmail} queued successfully!", toEmail);
            }
            catch (Exception e)
            {
                _logger.LogInformation(e, "Failure Email to {toEmail}", toEmail);
            }

            return Task.CompletedTask;
        }
    }
}
