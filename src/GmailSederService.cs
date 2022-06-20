using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace FuehrerscheinstelleAppointmentFinder;

public class GmailSederService : IEmailSender
{
    private readonly ILogger<GmailSederService> _logger;
    private readonly GmailOptions _gmailOptions;

    public GmailSederService(IOptions<AppOptions> optionsAccessor,
        ILogger<GmailSederService> logger)
    {
        Options = optionsAccessor.Value;
        _gmailOptions = Options.EmailSenderOptions.GmailOptions;
        _logger = logger;
    }

    public AppOptions Options { get; }
    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var fromAddress = new MailboxAddress(_gmailOptions.ApplicationName, _gmailOptions.UserId);
        var toAddress = new MailboxAddress(toEmail, toEmail);


        var message = new MimeMessage();
        message.To.Add(toAddress);
        message.From.Add(fromAddress);
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = body,
            TextBody = body
        };
        message.Body = bodyBuilder.ToMessageBody();
            

        var authorizationCodeFlow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets()
            {
                ClientId = _gmailOptions.ClientId,
                ClientSecret = _gmailOptions.ClientSecret
            },
        });

        var tokenResponse = await authorizationCodeFlow.RefreshTokenAsync(_gmailOptions.UserId, _gmailOptions.RefreshToken, CancellationToken.None);

        var credential = new UserCredential(authorizationCodeFlow, _gmailOptions.UserId, tokenResponse);

        var gmailService = new GmailService(new BaseClientService.Initializer()
        {
            ApplicationName = _gmailOptions.ApplicationName,
            HttpClientInitializer = credential
        });

        var gmailMessage = new Google.Apis.Gmail.v1.Data.Message
        {
            Raw = Base64UrlEncode(message.ToString())
        };
        await gmailService.Users.Messages.Send(gmailMessage, _gmailOptions.UserId).ExecuteAsync();
    }

    private static string Base64UrlEncode(string input)
    {
        var inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
        // Special "url-safe" base64 encode.
        return Convert.ToBase64String(inputBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .Replace("=", "");
    }
}