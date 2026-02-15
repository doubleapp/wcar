using System.Text.Json;

namespace Wcar.Config;

public class ConfigManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _dataDir;
    private readonly string _configPath;

    public ConfigManager() : this(GetDefaultDataDir()) { }

    public ConfigManager(string dataDir)
    {
        _dataDir = dataDir;
        _configPath = Path.Combine(_dataDir, "config.json");
    }

    public string DataDir => _dataDir;
    public string ConfigPath => _configPath;

    public static string GetDefaultDataDir()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WCAR");
    }

    public AppConfig Load()
    {
        EnsureDataDir();

        if (!File.Exists(_configPath))
            return new AppConfig();

        try
        {
            var json = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize<AppConfig>(json, JsonOptions)
                   ?? new AppConfig();
        }
        catch (JsonException)
        {
            HandleCorruptFile(_configPath);
            return new AppConfig();
        }
    }

    public void Save(AppConfig config)
    {
        EnsureDataDir();

        var json = JsonSerializer.Serialize(config, JsonOptions);
        var tmpPath = _configPath + ".tmp";

        File.WriteAllText(tmpPath, json);
        File.Move(tmpPath, _configPath, overwrite: true);
    }

    private void EnsureDataDir()
    {
        if (!Directory.Exists(_dataDir))
            Directory.CreateDirectory(_dataDir);
    }

    private static void HandleCorruptFile(string path)
    {
        var corruptPath = path + ".corrupt.json";
        if (File.Exists(corruptPath))
            File.Delete(corruptPath);
        File.Move(path, corruptPath);
    }
}
