using System.Text.Json;
using System.Text.Json.Serialization;
using InternetTechLab1.Models;

namespace InternetTechLab1.Services;

public sealed class OpenWeatherMapService
{
    private readonly AppSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly IAppLogger _logger;

    public OpenWeatherMapService(AppSettings settings, HttpClient httpClient, IAppLogger logger)
    {
        _settings = settings;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<WeatherFetchResult> FetchAsync(string city, string units, string language, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            return new WeatherFetchResult
            {
                IsSuccess = false,
                ErrorMessage = "Город не может быть пустым"
            };
        }

        if (string.IsNullOrWhiteSpace(_settings.OpenWeatherMap.ApiKey))
        {
            return new WeatherFetchResult
            {
                IsSuccess = false,
                ErrorMessage = "API ключ OpenWeatherMap не задан. Укажите ключ в appsettings.json"
            };
        }

        var normalizedUnits = NormalizeUnits(units, _settings.OpenWeatherMap.DefaultUnits);
        var normalizedLanguage = string.IsNullOrWhiteSpace(language)
            ? _settings.OpenWeatherMap.DefaultLanguage
            : language.Trim().ToLowerInvariant();

        var requestUrl = BuildRequestUrl(city.Trim(), normalizedUnits, normalizedLanguage);

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(Math.Max(5, _settings.OpenWeatherMap.RequestTimeoutSeconds)));

            using var response = await _httpClient.GetAsync(requestUrl, cts.Token);
            var rawJson = await response.Content.ReadAsStringAsync(cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                return new WeatherFetchResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка API: {(int)response.StatusCode} {response.ReasonPhrase}",
                    RawJson = rawJson
                };
            }

            if (string.IsNullOrWhiteSpace(rawJson))
            {
                return new WeatherFetchResult
                {
                    IsSuccess = false,
                    ErrorMessage = "API вернул пустой ответ"
                };
            }

            var payload = JsonSerializer.Deserialize<OpenWeatherMapResponse>(rawJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (payload?.Main == null || payload.Weather == null || payload.Weather.Count == 0)
            {
                return new WeatherFetchResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Не удалось разобрать ответ API",
                    RawJson = rawJson
                };
            }

            var record = new WeatherRecord
            {
                City = string.IsNullOrWhiteSpace(payload.Name) ? city.Trim() : payload.Name.Trim(),
                Country = payload.Sys?.Country?.Trim() ?? string.Empty,
                Temperature = payload.Main.Temp,
                FeelsLike = payload.Main.FeelsLike,
                Humidity = payload.Main.Humidity,
                Pressure = payload.Main.Pressure,
                WindSpeed = payload.Wind?.Speed ?? 0,
                Description = payload.Weather[0].Description?.Trim() ?? string.Empty,
                ApiTimestampUtc = payload.Dt > 0
                    ? DateTimeOffset.FromUnixTimeSeconds(payload.Dt).UtcDateTime
                    : DateTime.UtcNow,
                SavedAtUtc = DateTime.UtcNow,
                Units = normalizedUnits,
                RawJson = rawJson
            };

            _logger.Info($"Получена погода из API для города {record.City}");

            return new WeatherFetchResult
            {
                IsSuccess = true,
                Record = record,
                RawJson = rawJson
            };
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new WeatherFetchResult
            {
                IsSuccess = false,
                ErrorMessage = "Превышено время ожидания ответа API"
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.Error(ex, "Ошибка сети при обращении к OpenWeatherMap");

            return new WeatherFetchResult
            {
                IsSuccess = false,
                ErrorMessage = "Сетевая ошибка при запросе к API"
            };
        }
        catch (JsonException ex)
        {
            _logger.Error(ex, "Ошибка разбора JSON ответа OpenWeatherMap");

            return new WeatherFetchResult
            {
                IsSuccess = false,
                ErrorMessage = "Ошибка разбора JSON ответа API"
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Необработанная ошибка в OpenWeatherMapService");

            return new WeatherFetchResult
            {
                IsSuccess = false,
                ErrorMessage = "Неизвестная ошибка при запросе к API"
            };
        }
    }

    private string BuildRequestUrl(string city, string units, string language)
    {
        var query = new Dictionary<string, string>
        {
            ["q"] = city,
            ["appid"] = _settings.OpenWeatherMap.ApiKey,
            ["units"] = units,
            ["lang"] = language
        };

        var queryString = string.Join("&", query.Select(x =>
            $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value)}"));

        return $"{_settings.OpenWeatherMap.BaseUrl}?{queryString}";
    }

    private static string NormalizeUnits(string inputUnits, string fallback)
    {
        if (string.IsNullOrWhiteSpace(inputUnits))
        {
            return fallback;
        }

        var value = inputUnits.Trim().ToLowerInvariant();

        return value switch
        {
            "metric" => "metric",
            "imperial" => "imperial",
            "standard" => "standard",
            _ => fallback
        };
    }

    private sealed class OpenWeatherMapResponse
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("dt")]
        public long Dt { get; set; }

        [JsonPropertyName("main")]
        public MainInfo? Main { get; set; }

        [JsonPropertyName("wind")]
        public WindInfo? Wind { get; set; }

        [JsonPropertyName("weather")]
        public List<WeatherInfo>? Weather { get; set; }

        [JsonPropertyName("sys")]
        public SysInfo? Sys { get; set; }
    }

    private sealed class MainInfo
    {
        [JsonPropertyName("temp")]
        public double Temp { get; set; }

        [JsonPropertyName("feels_like")]
        public double FeelsLike { get; set; }

        [JsonPropertyName("humidity")]
        public int Humidity { get; set; }

        [JsonPropertyName("pressure")]
        public int Pressure { get; set; }
    }

    private sealed class WindInfo
    {
        [JsonPropertyName("speed")]
        public double Speed { get; set; }
    }

    private sealed class WeatherInfo
    {
        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }

    private sealed class SysInfo
    {
        [JsonPropertyName("country")]
        public string? Country { get; set; }
    }
}
