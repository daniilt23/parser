using InternetTechLab1.Models;
using LiteDB;

namespace InternetTechLab1.Data;

public sealed class ScrapeRepository
{
    private readonly AppSettings _settings;

    public ScrapeRepository(AppSettings settings)
    {
        _settings = settings;
    }

    public Task SaveAsync(ScrapeDocument document, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        StoragePathHelper.EnsureParentDirectory(_settings.DocumentDb.DatabasePath);

        using var database = new LiteDatabase(GetResolvedDatabasePath());
        var collection = database.GetCollection<ScrapeDocument>(_settings.DocumentDb.CollectionName);
        collection.EnsureIndex(x => x.ScrapedAtUtc);
        collection.Insert(document);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ScrapeDocument>> GetLatestAsync(int count, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        StoragePathHelper.EnsureParentDirectory(_settings.DocumentDb.DatabasePath);

        using var database = new LiteDatabase(GetResolvedDatabasePath());
        var collection = database.GetCollection<ScrapeDocument>(_settings.DocumentDb.CollectionName);

        var docs = collection
            .FindAll()
            .OrderByDescending(x => x.ScrapedAtUtc)
            .Take(count)
            .ToList();

        return Task.FromResult<IReadOnlyList<ScrapeDocument>>(docs);
    }

    public string GetResolvedDatabasePath()
    {
        return StoragePathHelper.ResolvePath(_settings.DocumentDb.DatabasePath);
    }
}
