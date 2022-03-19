namespace FuehrerscheinstelleAppointmentFinder;

internal interface IEmailSender
{
    AppOptions Options { get; } //Set with Secret Manager.
    Task SendEmailAsync(string toEmail, string subject, string message);
}