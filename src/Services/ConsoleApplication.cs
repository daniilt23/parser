using InternetTechLab1.Data;
using InternetTechLab1.Models;

namespace InternetTechLab1.Services;

public sealed class ConsoleApplication
{
    private readonly AppSettings _settings;
    private readonly WeatherRepository _weatherRepository;
    private readonly ScrapeRepository _scrapeRepository;
    private readonly OpenWeatherMapService _openWeatherMapService;
    private readonly ScrapingService _scrapingService;
    private readonly IAppLogger _logger;

    public ConsoleApplication(
        AppSettings settings,
        WeatherRepository weatherRepository,
        ScrapeRepository scrapeRepository,
        OpenWeatherMapService openWeatherMapService,
        ScrapingService scrapingService,
        IAppLogger logger)
    {
        _settings = settings;
        _weatherRepository = weatherRepository;
        _scrapeRepository = scrapeRepository;
        _openWeatherMapService = openWeatherMapService;
        _scrapingService = scrapingService;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        await _weatherRepository.EnsureDatabaseAsync(cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            ShowMenu();
            Console.Write("Выберите пункт: ");
            var choice = Console.ReadLine()?.Trim();
            Console.WriteLine();

            switch (choice)
            {
                case "1":
                    await ConfigureDatabasesAsync(cancellationToken);
                    break;
                case "2":
                    await RequestWeatherAsync(cancellationToken);
                    break;
                case "3":
                    await ShowWeatherRecordsAsync(cancellationToken);
                    break;
                case "4":
                    await RunScrapingAsync(cancellationToken);
                    break;
                case "5":
                    await ShowLatestScrapesAsync(cancellationToken);
                    break;
                case "0":
                    _logger.Info("Завершение работы приложения");
                    return;
                default:
                    Console.WriteLine("Неизвестная команда. Повторите ввод.");
                    break;
            }

            Console.WriteLine();
        }
    }

    private void ShowMenu()
    {
        Console.WriteLine("================ Меню действий ================");
        Console.WriteLine("1. Настроить/показать параметры подключения к БД");
        Console.WriteLine("2. Выполнить запрос к OpenWeatherMap");
        Console.WriteLine("3. Показать сохраненные API-записи (с фильтром)");
        Console.WriteLine("4. Выполнить scraping по URL");
        Console.WriteLine("5. Показать последние N результатов scraping");
        Console.WriteLine("0. Выход");
        Console.WriteLine("===================================================");
    }

    private async Task ConfigureDatabasesAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Текущие параметры БД:");
        Console.WriteLine($"SQLite: {_weatherRepository.GetResolvedDatabasePath()}");
        Console.WriteLine($"DocDB (LiteDB): {_scrapeRepository.GetResolvedDatabasePath()}");
        Console.WriteLine($"Коллекция DocDB: {_settings.DocumentDb.CollectionName}");
        Console.WriteLine();

        Console.Write("Изменить пути для текущего запуска? (y/n): ");
        var answer = Console.ReadLine()?.Trim().ToLowerInvariant();

        if (answer is not ("yes" or "да" or "y"))
        {
            return;
        }

        Console.Write($"Новый путь к SQLite-файлу [{_settings.RelationalDb.DatabasePath}]: ");
        var sqlitePath = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(sqlitePath))
        {
            _settings.RelationalDb.DatabasePath = sqlitePath.Trim();
        }

        Console.Write($"Новый путь к DocDB-файлу [{_settings.DocumentDb.DatabasePath}]: ");
        var docDbPath = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(docDbPath))
        {
            _settings.DocumentDb.DatabasePath = docDbPath.Trim();
        }

        Console.Write($"Имя коллекции DocDB [{_settings.DocumentDb.CollectionName}]: ");
        var collectionName = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(collectionName))
        {
            _settings.DocumentDb.CollectionName = collectionName.Trim();
        }

        await _weatherRepository.EnsureDatabaseAsync(cancellationToken);

        Console.WriteLine("Параметры обновлены.");
        Console.WriteLine($"SQLite: {_weatherRepository.GetResolvedDatabasePath()}");
        Console.WriteLine($"DocDB (LiteDB): {_scrapeRepository.GetResolvedDatabasePath()}");
    }

    private async Task RequestWeatherAsync(CancellationToken cancellationToken)
    {
        Console.Write("Введите город: ");
        var city = Console.ReadLine()?.Trim() ?? string.Empty;

        Console.Write($"Единицы измерения (metric/imperial/standard) [{_settings.OpenWeatherMap.DefaultUnits}]: ");
        var units = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(units))
        {
            units = _settings.OpenWeatherMap.DefaultUnits;
        }

        Console.Write($"Язык ответа [{_settings.OpenWeatherMap.DefaultLanguage}]: ");
        var language = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(language))
        {
            language = _settings.OpenWeatherMap.DefaultLanguage;
        }

        var result = await _openWeatherMapService.FetchAsync(city, units, language, cancellationToken);

        if (!result.IsSuccess || result.Record == null)
        {
            Console.WriteLine($"Ошибка: {result.ErrorMessage}");

            if (!string.IsNullOrWhiteSpace(result.RawJson))
            {
                Console.WriteLine("Raw ответ API:");
                Console.WriteLine(Truncate(result.RawJson, 1000));
            }

            return;
        }

        await _weatherRepository.AddAsync(result.Record, cancellationToken);

        Console.WriteLine("Данные из API получены и сохранены в SQLite.");
        Console.WriteLine($"Город: {result.Record.City}, страна: {result.Record.Country}");
        Console.WriteLine($"Температура: {result.Record.Temperature} {GetTemperatureUnitSymbol(result.Record.Units)}");
        Console.WriteLine($"Ощущается как: {result.Record.FeelsLike} {GetTemperatureUnitSymbol(result.Record.Units)}");
        Console.WriteLine($"Описание: {result.Record.Description}");
        Console.WriteLine($"Влажность: {result.Record.Humidity}%");
        Console.WriteLine($"Давление: {result.Record.Pressure} hPa");
        Console.WriteLine($"Ветер: {result.Record.WindSpeed} m/s");
        Console.WriteLine($"Время данных API (UTC): {result.Record.ApiTimestampUtc:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine();
        Console.WriteLine("Raw ответ API:");
        Console.WriteLine(Truncate(result.RawJson, 1000));
    }

    private async Task ShowWeatherRecordsAsync(CancellationToken cancellationToken)
    {
        Console.Write("Фильтр по городу (пусто = без фильтра): ");
        var cityFilter = Console.ReadLine();

        Console.Write("Количество записей (по умолчанию 20): ");
        var limitInput = Console.ReadLine();
        var limit = TryParsePositiveInt(limitInput, 20);

        var records = await _weatherRepository.GetAsync(cityFilter, limit, cancellationToken);

        if (records.Count == 0)
        {
            Console.WriteLine("Записи не найдены.");
            return;
        }

        Console.WriteLine($"Найдено записей: {records.Count}");

        foreach (var record in records)
        {
            Console.WriteLine(
                $"[{record.Id}] {record.SavedAtUtc:yyyy-MM-dd HH:mm:ss} UTC | {record.City}, {record.Country} | " +
                $"{record.Temperature} {GetTemperatureUnitSymbol(record.Units)} | {record.Description} | влажность {record.Humidity}%");
        }
    }

    private async Task RunScrapingAsync(CancellationToken cancellationToken)
    {
        Console.Write("Введите URL: ");
        var url = Console.ReadLine() ?? string.Empty;

        var result = await _scrapingService.ScrapeAsync(url, cancellationToken);
        await _scrapeRepository.SaveAsync(result.Document, cancellationToken);

        if (!result.IsSuccess)
        {
            Console.WriteLine($"Ошибка scraping: {result.ErrorMessage}");
            Console.WriteLine("Результат с ошибкой сохранен в DocDB.");
            return;
        }

        Console.WriteLine("Scraping выполнен и сохранен в DocDB.");
        Console.WriteLine($"Title: {result.Document.Title}");
        Console.WriteLine($"H1: {result.Document.H1}");
        Console.WriteLine($"Ссылок извлечено: {result.Document.Links.Count}");

        if (result.Document.Links.Count > 0)
        {
            Console.WriteLine("Первые ссылки:");
            foreach (var link in result.Document.Links)
            {
                Console.WriteLine($"- {link.Text} -> {link.Href}");
            }
        }
    }

    private async Task ShowLatestScrapesAsync(CancellationToken cancellationToken)
    {
        Console.Write("Сколько последних результатов показать (по умолчанию 5): ");
        var input = Console.ReadLine();
        var count = TryParsePositiveInt(input, 5);

        var documents = await _scrapeRepository.GetLatestAsync(count, cancellationToken);

        if (documents.Count == 0)
        {
            Console.WriteLine("Результаты scraping отсутствуют.");
            return;
        }

        Console.WriteLine($"Последние {documents.Count} результатов:");

        foreach (var document in documents)
        {
            var status = document.IsSuccess
                ? $"OK ({document.StatusCode})"
                : $"ERROR ({document.StatusCode}) {document.ErrorMessage}";

            Console.WriteLine($"[{document.ScrapedAtUtc:yyyy-MM-dd HH:mm:ss} UTC] {status}");
            Console.WriteLine($"URL: {document.Url}");
            Console.WriteLine($"Title: {Truncate(document.Title, 120)}");
            Console.WriteLine($"H1: {Truncate(document.H1, 120)}");
            Console.WriteLine($"Ссылок: {document.Links.Count}");
            Console.WriteLine();
        }
    }

    private static int TryParsePositiveInt(string? input, int defaultValue)
    {
        if (int.TryParse(input, out var value) && value > 0)
        {
            return value;
        }

        return defaultValue;
    }

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength] + "...";
    }

    private static string GetTemperatureUnitSymbol(string units)
    {
        return units.Trim().ToLowerInvariant() switch
        {
            "metric" => "°C",
            "imperial" => "°F",
            _ => "K"
        };
    }
}
