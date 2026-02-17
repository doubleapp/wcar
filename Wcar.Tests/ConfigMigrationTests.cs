using Wcar.Config;

namespace Wcar.Tests;

public class ConfigMigrationTests : IDisposable
{
    private readonly string _testDir;
    private readonly ConfigManager _manager;

    public ConfigMigrationTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "wcar_migration_test_" + Guid.NewGuid().ToString("N"));
        _manager = new ConfigManager(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, recursive: true);
    }

    [Fact]
    public void MigrateAndDeserialize_OldDictionaryFormat_MigratesToList()
    {
        var oldJson = """
        {
            "AutoSaveIntervalMinutes": 5,
            "AutoSaveEnabled": true,
            "Scripts": [],
            "TrackedApps": { "Chrome": true, "VSCode": true, "CMD": true },
            "AutoStartEnabled": false,
            "AutoRestoreEnabled": false
        }
        """;

        var config = _manager.MigrateAndDeserialize(oldJson);

        Assert.NotNull(config);
        Assert.IsType<List<TrackedApp>>(config.TrackedApps);
        Assert.Contains(config.TrackedApps, a => a.ProcessName == "chrome");
        Assert.Contains(config.TrackedApps, a => a.ProcessName == "Code");
        Assert.Contains(config.TrackedApps, a => a.ProcessName == "cmd");
    }

    [Fact]
    public void MigrateAndDeserialize_PowerShellKey_ProducesTwoEntries()
    {
        var oldJson = """
        {
            "TrackedApps": { "PowerShell": true }
        }
        """;

        var config = _manager.MigrateAndDeserialize(oldJson);

        Assert.NotNull(config);
        Assert.Contains(config.TrackedApps, a => a.ProcessName == "powershell");
        Assert.Contains(config.TrackedApps, a => a.ProcessName == "pwsh");
    }

    [Fact]
    public void MigrateAndDeserialize_DisabledApp_IsExcludedFromMigration()
    {
        var oldJson = """
        {
            "TrackedApps": { "Chrome": true, "VSCode": false }
        }
        """;

        var config = _manager.MigrateAndDeserialize(oldJson);

        Assert.NotNull(config);
        Assert.Contains(config.TrackedApps, a => a.ProcessName == "chrome");
        Assert.DoesNotContain(config.TrackedApps, a => a.ProcessName == "Code");
    }

    [Fact]
    public void MigrateAndDeserialize_NewListFormat_DoesNotMigrate()
    {
        var newJson = """
        {
            "TrackedApps": [
                { "DisplayName": "Google Chrome", "ProcessName": "chrome", "Enabled": true, "Launch": "LaunchOnce" }
            ]
        }
        """;

        var config = _manager.MigrateAndDeserialize(newJson);

        Assert.NotNull(config);
        Assert.Single(config.TrackedApps);
        Assert.Equal("chrome", config.TrackedApps[0].ProcessName);
    }

    [Fact]
    public void Load_OldFormatOnDisk_MigratesAndSavesNewFormat()
    {
        Directory.CreateDirectory(_testDir);
        var oldJson = """
        {
            "AutoSaveIntervalMinutes": 5,
            "AutoSaveEnabled": true,
            "Scripts": [],
            "TrackedApps": { "Chrome": true, "Explorer": true },
            "AutoStartEnabled": false,
            "AutoRestoreEnabled": false
        }
        """;
        File.WriteAllText(_manager.ConfigPath, oldJson);

        var config = _manager.Load();

        // Migrated config should be a list
        Assert.IsType<List<TrackedApp>>(config.TrackedApps);
        Assert.Contains(config.TrackedApps, a => a.ProcessName == "chrome");

        // The saved file should now use the new array format
        var savedJson = File.ReadAllText(_manager.ConfigPath);
        Assert.Contains("[", savedJson); // TrackedApps should be a JSON array
    }

    [Fact]
    public void MigrateAndDeserialize_DockerDesktopKey_Migrates()
    {
        var oldJson = """
        {
            "TrackedApps": { "DockerDesktop": true }
        }
        """;

        var config = _manager.MigrateAndDeserialize(oldJson);

        Assert.NotNull(config);
        Assert.Contains(config.TrackedApps, a => a.DisplayName == "Docker Desktop");
    }
}
