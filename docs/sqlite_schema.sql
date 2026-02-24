CREATE TABLE IF NOT EXISTS WeatherRecords (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    City TEXT NOT NULL,
    Country TEXT NOT NULL DEFAULT '',
    Temperature REAL NOT NULL,
    FeelsLike REAL NOT NULL,
    Humidity INTEGER NOT NULL,
    Pressure INTEGER NOT NULL,
    WindSpeed REAL NOT NULL,
    Description TEXT NOT NULL DEFAULT '',
    ApiTimestampUtc TEXT NOT NULL,
    SavedAtUtc TEXT NOT NULL,
    Units TEXT NOT NULL DEFAULT '',
    RawJson TEXT NOT NULL DEFAULT ''
);

CREATE INDEX IF NOT EXISTS IX_WeatherRecords_City ON WeatherRecords (City);
CREATE INDEX IF NOT EXISTS IX_WeatherRecords_SavedAtUtc ON WeatherRecords (SavedAtUtc);
