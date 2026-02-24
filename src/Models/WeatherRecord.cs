namespace InternetTechLab1.Models;

public sealed class WeatherRecord
{
    public int Id { get; set; }
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public double FeelsLike { get; set; }
    public int Humidity { get; set; }
    public int Pressure { get; set; }
    public double WindSpeed { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime ApiTimestampUtc { get; set; }
    public DateTime SavedAtUtc { get; set; }
    public string Units { get; set; } = string.Empty;
    public string RawJson { get; set; } = string.Empty;
}
