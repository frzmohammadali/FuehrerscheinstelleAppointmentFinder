using System.ComponentModel.DataAnnotations;

namespace FuehrerscheinstelleAppointmentFinder
{
    public class AppOptions
    {
        public string RetryIntervalInMinutes { get; set; }
        public string PrintIntervalInSeconds { get; set; }
        public string ZipCode { get; set; }
        public string DesiredAppointmentBefore { get; set; }
        public string RecipientEmail { get; set; }
        public EmailSenderOptions EmailSenderOptions { get; set; }
        public AppointmentBookingForm AppointmentBookingForm { get; set; }
        public ChromeDriverOptions ChromeDriverOptions { get; set; }
        
    }

    public class EmailSenderOptions
    {
        public SendGridOptions SendGridOptions { get; set; }
        public SmtpOptions SmtpOptions { get; set; }
        public GmailOptions GmailOptions { get; set; }
    }

    public class SendGridOptions
    {
        public string ApiKey { get; set; }
        public string SenderEmail { get; set; }
        public string SenderName { get; set; }
    }

    public class SmtpOptions
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public bool EnableSsl { get; set; }
        public string SenderEmail { get; set; }
        public string SenderName { get; set; }
        public string Password { get; set; }
    }

    public class GmailOptions
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string UserId { get; set; }
        public string ApplicationName { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        
    }

    public class AppointmentBookingForm
    {
        public bool Enabled { get; set; }

        [RegularExpression("^(Herr|Frau|Divers|Firma)$", ErrorMessage = "Invalid gender value. Must be one of [\"Herr\", \"Frau\", \"Divers\", \"Firma\"]")]
        public string Salutation { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailAddress { get; set; }
        public string PhoneNumber { get; set; }

        [RegularExpression(@"\d{2}\.\d{2}\.\d{4}", ErrorMessage = "Birthday must be in the format 'dd.mm.yyyy'.")]
        public string Birthday { get; set; }
    }

    public class ChromeDriverOptions
    {
        public bool Headless { get; set; }
    }

}
