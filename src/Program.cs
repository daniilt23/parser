using InternetTechLab1.Data;
using InternetTechLab1.Models;
using InternetTechLab1.Services;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: false)
    .Build();

var settings = configuration.Get<AppSettings>() ?? new AppSettings();
var logger = new AppLogger(settings);

using var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("InternetTechLab1/1.0");

var weatherRepository = new WeatherRepository(settings);
var scrapeRepository = new ScrapeRepository(settings);
var openWeatherMapService = new OpenWeatherMapService(settings, httpClient, logger);
var scrapingService = new ScrapingService(settings, httpClient, logger);

var app = new ConsoleApplication(
    settings,
    weatherRepository,
    scrapeRepository,
    openWeatherMapService,
    scrapingService,
    logger);

try
{
    logger.Info("Приложение запущено");
    await app.RunAsync();
}
catch (Exception ex)
{
    logger.Error(ex, "Критическая ошибка приложения");
    Console.WriteLine("Приложение завершено с ошибкой");
    Environment.ExitCode = 1;
}
