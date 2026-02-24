using InternetTechLab1.Models;
using Microsoft.EntityFrameworkCore;

namespace InternetTechLab1.Data;

public sealed class WeatherRepository
{
    private readonly AppSettings _settings;

    public WeatherRepository(AppSettings settings)
    {
        _settings = settings;
    }

    public async Task EnsureDatabaseAsync(CancellationToken cancellationToken = default)
    {
        using var context = CreateContext();
        await context.Database.EnsureCreatedAsync(cancellationToken);
    }

    public async Task AddAsync(WeatherRecord record, CancellationToken cancellationToken = default)
    {
        using var context = CreateContext();
        context.WeatherRecords.Add(record);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WeatherRecord>> GetAsync(string? cityFilter, int limit, CancellationToken cancellationToken = default)
    {
        using var context = CreateContext();

        var query = context.WeatherRecords.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(cityFilter))
        {
            query = query.Where(x => EF.Functions.Like(x.City, $"%{cityFilter.Trim()}%"));
        }

        return await query
            .OrderByDescending(x => x.SavedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public string GetResolvedDatabasePath()
    {
        return StoragePathHelper.ResolvePath(_settings.RelationalDb.DatabasePath);
    }

    private AppDbContext CreateContext()
    {
        StoragePathHelper.EnsureParentDirectory(_settings.RelationalDb.DatabasePath);

        var resolvedPath = StoragePathHelper.ResolvePath(_settings.RelationalDb.DatabasePath);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={resolvedPath}")
            .Options;

        return new AppDbContext(options);
    }
}
