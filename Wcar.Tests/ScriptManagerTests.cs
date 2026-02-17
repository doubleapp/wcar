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

    [Fact]
    public void AddScript_WithShell_StoresShellType()
    {
        _manager.AddScript("cmd-test", "dir", ScriptShell.Cmd);

        var scripts = _manager.GetScripts();
        Assert.Single(scripts);
        Assert.Equal(ScriptShell.Cmd, scripts[0].Shell);
    }

    [Fact]
    public void AddScript_WithDescription_StoresDescription()
    {
        _manager.AddScript("test", "echo hi", ScriptShell.Bash, "Says hi");

        var scripts = _manager.GetScripts();
        Assert.Single(scripts);
        Assert.Equal("Says hi", scripts[0].Description);
    }

    [Fact]
    public void AddScript_DefaultShell_IsPowerShell()
    {
        _manager.AddScript("test", "Write-Host Hello");

        var scripts = _manager.GetScripts();
        Assert.Equal(ScriptShell.PowerShell, scripts[0].Shell);
    }

    [Fact]
    public void EditScript_UpdatesShellAndDescription()
    {
        _manager.AddScript("test", "Write-Host Hello");

        _manager.EditScript("test", "echo hello", ScriptShell.Cmd, "Updated");

        var scripts = _manager.GetScripts();
        Assert.Equal("echo hello", scripts[0].Command);
        Assert.Equal(ScriptShell.Cmd, scripts[0].Shell);
        Assert.Equal("Updated", scripts[0].Description);
    }
}
