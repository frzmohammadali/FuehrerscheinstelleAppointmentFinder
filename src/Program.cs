using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Text;
using FuehrerscheinstelleAppointmentFinder;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using WebDriverManager.Helpers;
using Microsoft.Extensions.Options;

bool keepRunning = true;
long successfulTryCount = 0;
long unsuccessfulTryCount = 0;

Console.CancelKeyPress += Console_CancelKeyPress;

void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
{
    e.Cancel = true;
    keepRunning = false;
}

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("appsettings.local.json", optional: true)
    .Build();

var app = Host.CreateDefaultBuilder()
    .ConfigureServices(services =>
    {
        services.AddSingleton<IConfiguration>(config);
        services.AddOptions<AppOptions>()
        .Bind(config.GetSection(nameof(AppOptions)))
        .ValidateDataAnnotations();
        services.AddSingleton<EmailSender>();
        services.AddTransient<ChromeDriver>(sp =>
        {
            new DriverManager().SetUpDriver(new ChromeConfig(), VersionResolveStrategy.MatchingBrowser);
            var options = new ChromeOptions();
            options.AddArgument("headless");
            var driver = new ChromeDriver(options);
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
            var _logger = sp.GetRequiredService<ILogger<Program>>();
            driver.GetDevToolsSession().LogMessage += DevToolsSession_LogMessage;
            void DevToolsSession_LogMessage(object? sender, OpenQA.Selenium.DevTools.DevToolsSessionLogMessageEventArgs e)
            {
                _logger.LogDebug(e.Level.ToString(), e.Message);
            }
            return driver;
        });
    })
    .Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
var options = app.Services.GetRequiredService<IOptions<AppOptions>>().Value;

int retryIntervalInMinutes = int.Parse(options.RetryIntervalInMinutes);
int printIntervalInSeconds = int.Parse(options.PrintIntervalInSeconds);

logger.LogInformation("ChromeDriver is registered successfully!");
logger.LogInformation("--== bot has started ==--");

DateTimeOffset? lastTry = null;
DateTimeOffset? lastPrint = null;
object _lock = new object();

while (keepRunning)
{
    if (lastTry is not null && (((DateTimeOffset.Now - lastTry.Value).TotalMinutes) < retryIntervalInMinutes))
    {
        if (lastPrint is null
            || (DateTimeOffset.Now - lastPrint.Value).TotalSeconds >= printIntervalInSeconds)
        {
            logger.LogInformation("Current time {t}", DateTimeOffset.Now.ToString("G"));
            logger.LogInformation("Next try in less than {min} minutes...", retryIntervalInMinutes - ((int)(DateTimeOffset.Now - lastTry.Value).TotalMinutes));
            lastPrint = DateTimeOffset.Now;
        }
        continue;
    }

    try
    {
        lock (_lock)
        {
            using (var driver = app.Services.GetRequiredService<ChromeDriver>())
            {
                driver.Navigate().GoToUrl("https://www.kaiserslautern.de/serviceportal/dl/037455/index.html.de");

                var link = driver.FindElement(By.XPath("/html/body/div[7]/div/div/div[2]/div[2]/div[5]/div/ul/li[4]/a"));
                link.Click();

                var zipCodeInput = driver.FindElement(By.XPath("/html/body/div[2]/div[2]/form/div[3]/div[2]/div[1]/div/div/input"));
                zipCodeInput.SendKeys(options.ZipCode);

                var weiterButton = driver.FindElement(By.XPath("/html/body/div[2]/div[2]/form/div[3]/div[2]/div[2]/button"));
                weiterButton.Click();

                var chooseOrderButton = driver.FindElement(By.XPath("/html/body/div[2]/div[2]/form/div[3]/div[3]/div/div[1]/div[12]/div/div[2]/span[3]"));
                chooseOrderButton.Click();

                var weiterButton2 = driver.FindElement(By.XPath("/html/body/div[2]/div[2]/form/div[3]/div[3]/div/div[3]/button"));
                weiterButton2.Click();

                var dates = driver.FindElements(By.CssSelector(".smart-date")).ToArray();
                var datesFiltered = dates.Select(s => s.GetAttribute("id").ToLowerInvariant())
                    .Select(s => s.Substring("day-".Length))
                    .Select(s => DateTimeOffset.Parse(s))
                    .Where(i => i.CompareTo(DateTimeOffset.Parse(options.DesiredAppointmentBefore)) <= 0)
                    .ToArray();

                if (datesFiltered.Any())
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("Available new dates:");
                    sb.AppendLine("<br /><br />");
                    foreach (var item in datesFiltered)
                    {
                        sb.AppendLine(item.ToString("yyyy-MM-dd"));
                        sb.AppendLine("<br />");
                    }
                    var message = sb.ToString();
                    _ = app.Services.GetRequiredService<EmailSender>()
                        .SendEmailAsync(options.RecipientEmail, "New Fuehrerscheinstelle appointment available", message);
                    Console.WriteLine(message.Replace("<br />", ""));
                }

                lastTry = DateTimeOffset.Now;
                successfulTryCount++;
                
            }
        }
    }
    catch (Exception e)
    {
        logger.LogError(e, "An unhandled exception occurred");
        unsuccessfulTryCount++;
    }
    finally
    {
        logger.LogInformation("--== successful tries: {su_count} ==--", successfulTryCount);
        logger.LogInformation("--== unsuccessful tries: {unsu_count} ==--", unsuccessfulTryCount);
    }

    
}

logger.LogInformation("--== ChromeDriver is successfully closed ==--");
logger.LogInformation("--== application shuting down ==--");
Console.WriteLine("\n");
Console.WriteLine("Press any key to exit...");
Console.ReadKey();
