using LiteDB;

namespace InternetTechLab1.Models;

public sealed class ScrapeDocument
{
    [BsonId]
    public ObjectId Id { get; set; } = ObjectId.NewObjectId();

    public string Url { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string H1 { get; set; } = string.Empty;
    public List<ScrapeLink> Links { get; set; } = new();
    public DateTime ScrapedAtUtc { get; set; }
}

public sealed class ScrapeLink
{
    public string Text { get; set; } = string.Empty;
    public string Href { get; set; } = string.Empty;
}
