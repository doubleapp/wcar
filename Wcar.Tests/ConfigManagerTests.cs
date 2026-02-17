using Wcar.Config;

namespace Wcar.Tests;

public class ConfigManagerTests : IDisposable
{
    private readonly string _testDir;
    private readonly ConfigManager _manager;

    public ConfigManagerTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "wcar_test_" + Guid.NewGuid().ToString("N"));
        _manager = new ConfigManager(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, recursive: true);
    }

    [Fact]
    public void Load_NoFile_ReturnsDefaults()
    {
        var config = _manager.Load();

        Assert.True(config.AutoSaveEnabled);
        Assert.Equal(5, config.AutoSaveIntervalMinutes);
        Assert.Empty(config.Scripts);
        Assert.Contains(config.TrackedApps, a => a.ProcessName == "chrome" && a.Enabled);
    }

    [Fact]
    public void SaveAndLoad_RoundTrips()
    {
        var config = new AppConfig { AutoSaveIntervalMinutes = 10 };
        config.Scripts.Add(new ScriptEntry("test", "echo hello"));

        _manager.Save(config);
        var loaded = _manager.Load();

        Assert.Equal(10, loaded.AutoSaveIntervalMinutes);
        Assert.Single(loaded.Scripts);
        Assert.Equal("test", loaded.Scripts[0].Name);
    }

    [Fact]
    public void Load_CorruptJson_ReturnsDefaultsAndRenamesFile()
    {
        Directory.CreateDirectory(_testDir);
        File.WriteAllText(_manager.ConfigPath, "{{not valid json!!");

        var config = _manager.Load();

        Assert.True(config.AutoSaveEnabled);
        Assert.True(File.Exists(_manager.ConfigPath + ".corrupt.json"));
        Assert.False(File.Exists(_manager.ConfigPath));
    }

    [Fact]
    public void Save_CreatesDataDirectory()
    {
        Assert.False(Directory.Exists(_testDir));

        _manager.Save(new AppConfig());

        Assert.True(Directory.Exists(_testDir));
        Assert.True(File.Exists(_manager.ConfigPath));
    }

    [Fact]
    public void Save_AtomicWrite_NoPartialFile()
    {
        _manager.Save(new AppConfig());

        var tmpPath = _manager.ConfigPath + ".tmp";
        Assert.False(File.Exists(tmpPath));
        Assert.True(File.Exists(_manager.ConfigPath));
    }

    [Fact]
    public void Load_LegacyConfig_DefaultsNewScriptFields()
    {
        // Simulate a v1 config that has scripts without Shell/Description
        // and old-style TrackedApps dictionary — migration should work transparently
        var legacyJson = """
        {
            "AutoSaveIntervalMinutes": 5,
            "AutoSaveEnabled": true,
            "Scripts": [
                { "Name": "legacy", "Command": "echo hello" }
            ],
            "TrackedApps": { "Chrome": true },
            "AutoStartEnabled": false,
            "AutoRestoreEnabled": false
        }
        """;
        Directory.CreateDirectory(_testDir);
        File.WriteAllText(_manager.ConfigPath, legacyJson);

        var config = _manager.Load();

        Assert.Single(config.Scripts);
        Assert.Equal(ScriptShell.PowerShell, config.Scripts[0].Shell);
        Assert.Equal("", config.Scripts[0].Description);
        // TrackedApps should have been migrated to new list format
        Assert.Contains(config.TrackedApps, a => a.ProcessName == "chrome");
    }
}
