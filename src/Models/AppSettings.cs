namespace InternetTechLab1.Models;

public sealed class AppSettings
{
    public RelationalDbSettings RelationalDb { get; set; } = new();
    public DocumentDbSettings DocumentDb { get; set; } = new();
    public OpenWeatherMapSettings OpenWeatherMap { get; set; } = new();
    public ScrapingSettings Scraping { get; set; } = new();
    public LoggingSettings Logging { get; set; } = new();
}

public sealed class RelationalDbSettings
{
    public string DatabasePath { get; set; } = "data/weather.db";
}

public sealed class DocumentDbSettings
{
    public string DatabasePath { get; set; } = "data/scraping.db";
    public string CollectionName { get; set; } = "scrape_results";
}

public sealed class OpenWeatherMapSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.openweathermap.org/data/2.5/weather";
    public string DefaultUnits { get; set; } = "metric";
    public string DefaultLanguage { get; set; } = "ru";
    public int RequestTimeoutSeconds { get; set; } = 20;
}

public sealed class ScrapingSettings
{
    public int MaxLinks { get; set; } = 10;
    public int RequestTimeoutSeconds { get; set; } = 20;
}

public sealed class LoggingSettings
{
    public string FilePath { get; set; } = "logs/app.log";
}
