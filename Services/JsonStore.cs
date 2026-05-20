using System.IO;
using System.Text.Json;

namespace HorizonXIBackupApp.Services;

public static class JsonStore
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static T? LoadOrDefault<T>(string path, T fallback) where T : class, new()
    {
        if (!File.Exists(path)) return fallback;
        var raw = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(raw)) return fallback;
        return JsonSerializer.Deserialize<T>(raw, Options) ?? fallback;
    }

    public static void Save<T>(string path, T value)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(value, Options);
        File.WriteAllText(path, json);
    }
}
