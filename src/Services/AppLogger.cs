using InternetTechLab1.Data;
using InternetTechLab1.Models;

namespace InternetTechLab1.Services;

public interface IAppLogger
{
    void Info(string message);
    void Error(string message);
    void Error(Exception exception, string message);
}

public sealed class AppLogger : IAppLogger
{
    private readonly object _lock = new();
    private readonly string _filePath;

    public AppLogger(AppSettings settings)
    {
        _filePath = StoragePathHelper.ResolvePath(settings.Logging.FilePath);
        StoragePathHelper.EnsureParentDirectory(_filePath);
    }

    public void Info(string message)
    {
        Write("INFO", message);
    }

    public void Error(string message)
    {
        Write("ERROR", message);
    }

    public void Error(Exception exception, string message)
    {
        Write("ERROR", $"{message}. {exception.GetType().Name}: {exception.Message}");
    }

    private void Write(string level, string message)
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";

        Console.WriteLine(line);

        lock (_lock)
        {
            File.AppendAllText(_filePath, line + Environment.NewLine);
        }
    }
}
