namespace InternetTechLab1.Data;

public static class StoragePathHelper
{
    public static string ResolvePath(string path)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), path));
    }

    public static void EnsureParentDirectory(string filePath)
    {
        var resolved = ResolvePath(filePath);
        var directory = Path.GetDirectoryName(resolved);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
