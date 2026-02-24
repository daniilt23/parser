using System.Text.RegularExpressions;
using HtmlAgilityPack;
using InternetTechLab1.Models;

namespace InternetTechLab1.Services;

public sealed class ScrapingService
{
    private readonly AppSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly IAppLogger _logger;

    public ScrapingService(AppSettings settings, HttpClient httpClient, IAppLogger logger)
    {
        _settings = settings;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ScrapeResult> ScrapeAsync(string url, CancellationToken cancellationToken = default)
    {
        var document = new ScrapeDocument
        {
            Url = url?.Trim() ?? string.Empty,
            ScrapedAtUtc = DateTime.UtcNow
        };

        if (!TryBuildUri(url, out var uri))
        {
            document.IsSuccess = false;
            document.ErrorMessage = "Некорректный URL. Используйте абсолютный адрес с http/https";

            return new ScrapeResult
            {
                IsSuccess = false,
                ErrorMessage = document.ErrorMessage,
                Document = document
            };
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(Math.Max(5, _settings.Scraping.RequestTimeoutSeconds)));

            using var response = await _httpClient.GetAsync(uri, cts.Token);
            document.StatusCode = (int)response.StatusCode;

            if (!response.IsSuccessStatusCode)
            {
                document.IsSuccess = false;
                document.ErrorMessage = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";

                return new ScrapeResult
                {
                    IsSuccess = false,
                    ErrorMessage = document.ErrorMessage,
                    Document = document
                };
            }

            var html = await response.Content.ReadAsStringAsync(cts.Token);

            if (string.IsNullOrWhiteSpace(html))
            {
                document.IsSuccess = false;
                document.ErrorMessage = "Получена пустая HTML-страница";

                return new ScrapeResult
                {
                    IsSuccess = false,
                    ErrorMessage = document.ErrorMessage,
                    Document = document
                };
            }

            FillDocumentFromHtml(document, uri, html);
            document.IsSuccess = true;

            _logger.Info($"Scraping выполнен для URL: {document.Url}");

            return new ScrapeResult
            {
                IsSuccess = true,
                Document = document
            };
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            document.IsSuccess = false;
            document.ErrorMessage = "Превышено время ожидания ответа страницы";

            return new ScrapeResult
            {
                IsSuccess = false,
                ErrorMessage = document.ErrorMessage,
                Document = document
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.Error(ex, "Ошибка сети во время scraping");
            document.IsSuccess = false;
            document.ErrorMessage = "Сетевая ошибка при загрузке страницы";

            return new ScrapeResult
            {
                IsSuccess = false,
                ErrorMessage = document.ErrorMessage,
                Document = document
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Необработанная ошибка во время scraping");
            document.IsSuccess = false;
            document.ErrorMessage = "Ошибка обработки HTML-страницы";

            return new ScrapeResult
            {
                IsSuccess = false,
                ErrorMessage = document.ErrorMessage,
                Document = document
            };
        }
    }

    private void FillDocumentFromHtml(ScrapeDocument document, Uri pageUri, string html)
    {
        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(html);

        document.Title = CleanText(htmlDocument.DocumentNode.SelectSingleNode("//title")?.InnerText);
        document.H1 = CleanText(htmlDocument.DocumentNode.SelectSingleNode("//h1")?.InnerText);
        document.Links = ExtractLinks(htmlDocument, pageUri).ToList();
    }

    private IEnumerable<ScrapeLink> ExtractLinks(HtmlDocument htmlDocument, Uri pageUri)
    {
        var result = new List<ScrapeLink>();
        var nodes = htmlDocument.DocumentNode.SelectNodes("//a[@href]");

        if (nodes == null)
            return result;

        var uniqueHrefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var node in nodes)
        {
            if (result.Count >= _settings.Scraping.MaxLinks)
                break;

            var href = node.GetAttributeValue("href", string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(href))
                continue;

            if (Uri.TryCreate(pageUri, href, out var absolute))
                href = absolute.ToString();

            if (!uniqueHrefs.Add(href))
                continue;

            var text = CleanText(node.InnerText);

            result.Add(new ScrapeLink
            {
                Text = string.IsNullOrWhiteSpace(text) ? href : text,
                Href = href
            });
        }

        return result;
    }

    private static string CleanText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var decoded = HtmlEntity.DeEntitize(value);
        return Regex.Replace(decoded, "\\s+", " ").Trim();
    }

    private static bool TryBuildUri(string? url, out Uri uri)
    {
        uri = default!;

        if (!Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var parsedUri))
            return false;

        if (parsedUri.Scheme is not ("http" or "https"))
            return false;

        uri = parsedUri;
        return true;
    }
}