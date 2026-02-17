using System.Text.Json;
using System.Text.Json.Nodes;

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
            var config = MigrateAndDeserialize(json);
            return config ?? new AppConfig();
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

    /// <summary>
    /// Detects and migrates old dictionary-format TrackedApps to the new List format.
    /// Old: "TrackedApps": { "Chrome": true, ... }
    /// New: "TrackedApps": [ { "DisplayName": "...", ... }, ... ]
    /// </summary>
    internal AppConfig? MigrateAndDeserialize(string json)
    {
        var doc = JsonNode.Parse(json);
        if (doc == null) return null;

        var trackedAppsNode = doc["TrackedApps"];

        if (trackedAppsNode is JsonObject oldDict)
        {
            var newList = MigrateDictionaryToList(oldDict);
            doc["TrackedApps"] = newList;
            var migratedJson = doc.ToJsonString();

            var migrated = JsonSerializer.Deserialize<AppConfig>(migratedJson, JsonOptions) ?? new AppConfig();
            migrated.TrackedApps = newList.Deserialize<List<TrackedApp>>(JsonOptions)
                                   ?? AppConfig.DefaultTrackedApps();
            Save(migrated);
            return migrated;
        }

        return JsonSerializer.Deserialize<AppConfig>(json, JsonOptions);
    }

    private static JsonArray MigrateDictionaryToList(JsonObject oldDict)
    {
        var knownMappings = new Dictionary<string, List<TrackedApp>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Chrome"]   = new() { new() { DisplayName = "Google Chrome",      ProcessName = "chrome",         ExecutablePath = null,             Launch = LaunchStrategy.LaunchOnce } },
            ["VSCode"]   = new() { new() { DisplayName = "Visual Studio Code", ProcessName = "Code",           ExecutablePath = null,             Launch = LaunchStrategy.LaunchOnce } },
            ["CMD"]      = new() { new() { DisplayName = "Command Prompt",     ProcessName = "cmd",            ExecutablePath = "cmd.exe",        Launch = LaunchStrategy.LaunchPerWindow } },
            // Old "PowerShell" key produces two entries: powershell + pwsh
            ["PowerShell"] = new()
            {
                new() { DisplayName = "PowerShell",      ProcessName = "powershell", ExecutablePath = "powershell.exe", Launch = LaunchStrategy.LaunchPerWindow },
                new() { DisplayName = "PowerShell Core", ProcessName = "pwsh",       ExecutablePath = "pwsh.exe",       Launch = LaunchStrategy.LaunchPerWindow }
            },
            ["Explorer"]      = new() { new() { DisplayName = "File Explorer",  ProcessName = "explorer",       ExecutablePath = "explorer.exe",  Launch = LaunchStrategy.LaunchPerWindow } },
            ["DockerDesktop"] = new() { new() { DisplayName = "Docker Desktop", ProcessName = "Docker Desktop", ExecutablePath = null,            Launch = LaunchStrategy.LaunchOnce } },
        };

        var result = new JsonArray();

        foreach (var kvp in oldDict)
        {
            var enabled = kvp.Value?.GetValue<bool>() ?? true;
            if (!enabled) continue; // Skip disabled apps during migration

            if (knownMappings.TryGetValue(kvp.Key, out var apps))
            {
                foreach (var app in apps)
                {
                    app.Enabled = true;
                    var node = JsonSerializer.SerializeToNode(app, JsonOptions);
                    if (node != null) result.Add(node);
                }
            }
            else
            {
                var app = new TrackedApp
                {
                    DisplayName = kvp.Key,
                    ProcessName = kvp.Key.ToLowerInvariant(),
                    Enabled = true,
                    Launch = LaunchStrategy.LaunchOnce
                };
                var node = JsonSerializer.SerializeToNode(app, JsonOptions);
                if (node != null) result.Add(node);
            }
        }

        return result;
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
