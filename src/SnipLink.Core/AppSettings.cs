using System.Text.Json;

namespace SnipLink;

public sealed class AppSettings
{
    public string WebhookUrl { get; set; } = "";

    public bool Shorten { get; set; }

    public int DelaySeconds { get; set; }

    public bool Autostart { get; set; }
}

public static class SettingsStore
{
    static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SnipLink",
        "settings.json");

    public static AppSettings Load() => Load(FilePath);

    public static AppSettings Load(string path)
    {
        try
        {
            if (!File.Exists(path))
                return new AppSettings();

            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), Json) ?? new AppSettings();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings) => Save(settings, FilePath);

    public static void Save(AppSettings settings, string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(settings, Json));
        File.Move(temp, path, overwrite: true);
    }
}
