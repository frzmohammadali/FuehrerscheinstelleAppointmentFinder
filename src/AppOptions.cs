using System.ComponentModel.DataAnnotations;

namespace FuehrerscheinstelleAppointmentFinder
{
    public class AppOptions
    {
        [Required]
        public string SendGridKey { get; set; } = null!;

        [Required]
        public string RetryIntervalInMinutes { get; set; } = null!;

        [Required]
        public string PrintIntervalInSeconds { get; set; } = null!;

        [Required]
        public string ZipCode { get; set; } = null!;

        [Required]
        public string DesiredAppointmentBefore { get; set; } = null!;

        [Required]
        public string SenderEmail { get; set; } = null!;

        [Required]
        public string SenderName { get; set; } = null!;

        [Required]
        public string RecipientEmail { get; set; } = null!;
    }
}
