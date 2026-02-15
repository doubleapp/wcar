using Wcar.Config;
using Wcar.Scripts;

namespace Wcar.Tests;

public class ScriptManagerTests : IDisposable
{
    private readonly string _testDir;
    private readonly ScriptManager _manager;

    public ScriptManagerTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "wcar_script_test_" + Guid.NewGuid().ToString("N"));
        var configManager = new ConfigManager(_testDir);
        _manager = new ScriptManager(configManager);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, recursive: true);
    }

    [Fact]
    public void AddScript_NewScript_ReturnsTrue()
    {
        var result = _manager.AddScript("test", "Write-Host Hello");

        Assert.True(result);
        var scripts = _manager.GetScripts();
        Assert.Single(scripts);
        Assert.Equal("test", scripts[0].Name);
        Assert.Equal("Write-Host Hello", scripts[0].Command);
    }

    [Fact]
    public void AddScript_DuplicateName_ReturnsFalse()
    {
        _manager.AddScript("test", "Write-Host Hello");
        var result = _manager.AddScript("test", "Write-Host World");

        Assert.False(result);
        Assert.Single(_manager.GetScripts());
    }

    [Fact]
    public void RemoveScript_ExistingScript_ReturnsTrueAndRemoves()
    {
        _manager.AddScript("test", "Write-Host Hello");

        var result = _manager.RemoveScript("test");

        Assert.True(result);
        Assert.Empty(_manager.GetScripts());
    }
}
