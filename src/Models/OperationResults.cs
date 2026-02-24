namespace InternetTechLab1.Models;

public sealed class WeatherFetchResult
{
    public bool IsSuccess { get; init; }
    public string ErrorMessage { get; init; } = string.Empty;
    public WeatherRecord? Record { get; init; }
    public string RawJson { get; init; } = string.Empty;
}

public sealed class ScrapeResult
{
    public bool IsSuccess { get; init; }
    public string ErrorMessage { get; init; } = string.Empty;
    public ScrapeDocument Document { get; init; } = new();
}
