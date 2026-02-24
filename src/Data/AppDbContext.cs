using InternetTechLab1.Models;
using Microsoft.EntityFrameworkCore;

namespace InternetTechLab1.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<WeatherRecord> WeatherRecords => Set<WeatherRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WeatherRecord>(entity =>
        {
            entity.ToTable("WeatherRecords");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.City).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Country).HasMaxLength(8);
            entity.Property(x => x.Description).HasMaxLength(512);
            entity.Property(x => x.Units).HasMaxLength(20);
            entity.Property(x => x.RawJson).HasColumnType("TEXT");
            entity.HasIndex(x => x.City);
            entity.HasIndex(x => x.SavedAtUtc);
        });
    }
}
