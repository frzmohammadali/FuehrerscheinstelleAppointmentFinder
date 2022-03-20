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

var keepRunning = true;
long successfulTryCount = 0;
long unsuccessfulTryCount = 0;

Console.CancelKeyPress += ConsoleCancelKeyPress;

void ConsoleCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
{
    Console.WriteLine("\n--== application shutting down... ==--\n");
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
        //services.AddSingleton<IEmailSender, EmailSenderSendGrid>();
        services.AddSingleton<IEmailSender, EmailSenderSmtp>();
        services.AddTransient<ChromeDriver>(sp =>
        {
            var chromeDriverOptions = sp.GetRequiredService<IOptions<AppOptions>>().Value.ChromeDriverOptions;
            new DriverManager().SetUpDriver(new ChromeConfig(), VersionResolveStrategy.MatchingBrowser);
            var options = new ChromeOptions();
            if (chromeDriverOptions.Headless) options.AddArgument("headless");
            var driver = new ChromeDriver(options);
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
            var _logger = sp.GetRequiredService<ILogger<Program>>();
            driver.GetDevToolsSession().LogMessage += DevToolsSessionLogMessage;
            void DevToolsSessionLogMessage(object? sender, OpenQA.Selenium.DevTools.DevToolsSessionLogMessageEventArgs e)
            {
                _logger.LogDebug(e.Level.ToString(), e.Message);
            }
            return driver;
        });
    })
    .Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
var options = app.Services.GetRequiredService<IOptions<AppOptions>>().Value;

var retryIntervalInMinutes = int.Parse(options.RetryIntervalInMinutes);
var printIntervalInSeconds = int.Parse(options.PrintIntervalInSeconds);

logger.LogInformation("ChromeDriver is registered successfully!");
logger.LogInformation("--== bot has started ==--");

DateTimeOffset? lastTry = null;
DateTimeOffset? lastPrint = null;
var _lock = new object();

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
            using var driver = app.Services.GetRequiredService<ChromeDriver>();
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
                .Select(s => s["day-".Length..])
                .Select(DateTimeOffset.Parse)
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

                if (options.AppointmentBookingForm.Enabled)
                {
                    var chosenDate = datesFiltered.First();
                    var dateToClick = driver.FindElement(By.CssSelector($"a#day-{chosenDate:yyyy-MM-dd}"));
                    dateToClick.Click();

                    var timeDialog =
                            driver
                                .FindElements(By.CssSelector("div.smart-date-list"))
                                .FirstOrDefault(e => e.Displayed);
                    if (timeDialog is null)
                        throw new OperationCanceledException("time dialog is not visible on page after click on a date");

                    var timeLinkToClick = timeDialog.FindElements(By.CssSelector("a.ui.primary.button")).FirstOrDefault();
                    if (timeLinkToClick is null)
                        throw new OperationCanceledException("a visible link to a time to select is not found");
                    var actualSelectedTime = timeLinkToClick.Text;
                    timeLinkToClick.Click();

                    var firstNameInput =
                        driver.FindElement(By.XPath("/html/body/div[2]/div[2]/form/div[2]/div[1]/div/div[1]/div[3]/input"));
                    firstNameInput.SendKeys(options.AppointmentBookingForm.FirstName);

                    var lastNameInput =
                        driver.FindElement(By.XPath("/html/body/div[2]/div[2]/form/div[2]/div[1]/div/div[1]/div[4]/input"));
                    lastNameInput.SendKeys(options.AppointmentBookingForm.LastName);

                    var emailInput =
                        driver.FindElement(By.XPath("/html/body/div[2]/div[2]/form/div[2]/div[1]/div/div[1]/div[5]/input"));
                    emailInput.SendKeys(options.AppointmentBookingForm.EmailAddress);

                    var emailAgainInput =
                        driver.FindElement(By.XPath("/html/body/div[2]/div[2]/form/div[2]/div[1]/div/div[1]/div[6]/input"));
                    emailAgainInput.SendKeys(options.AppointmentBookingForm.EmailAddress);

                    var phoneInput =
                        driver.FindElement(By.XPath("/html/body/div[2]/div[2]/form/div[2]/div[1]/div/div[1]/div[7]/input"));
                    phoneInput.SendKeys(options.AppointmentBookingForm.PhoneNumber);

                    var birthdayInput =
                        driver.FindElement(By.XPath("/html/body/div[2]/div[2]/form/div[2]/div[1]/div/div[1]/div[8]/input"));
                    birthdayInput.SendKeys(options.AppointmentBookingForm.Birthday);

                    var agreeToPolicyInput =
                        driver.FindElement(By.XPath("/html/body/div[2]/div[2]/form/div[2]/div[1]/div/div[1]/div[9]/input"));
                    agreeToPolicyInput.Click();

                    var submitButton =
                        driver.FindElement(By.XPath("/html/body/div[2]/div[2]/form/div[2]/div[1]/div/div[2]/button"));
                    submitButton.Click();

                    Thread.Sleep(2000);

                    sb.AppendLine("<br /><br />");
                    sb.Append(
                        $"And an appointment on {chosenDate.ToString("D")} " +
                        $"at {actualSelectedTime} is successfully booked!");
                }

                var message = sb.ToString();
                _ = app.Services.GetRequiredService<IEmailSender>()
                    .SendEmailAsync(options.RecipientEmail, "New Fuehrerscheinstelle appointment available", message);
                logger.LogInformation(message.Replace("<br />", ""));
            }

            lastTry = DateTimeOffset.Now;
            successfulTryCount++;
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

await app.StopAsync(TimeSpan.FromSeconds(5));
await app.WaitForShutdownAsync();

logger.LogInformation("--== ChromeDriver is successfully closed ==--");
logger.LogInformation("--== application successfully exited ==--");

await Task.Delay(1000);
Console.WriteLine("\n");
Console.WriteLine("Press any key to exit...");
Console.ReadKey();
