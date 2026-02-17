using System.Text.Json;
using Wcar.Config;

namespace Wcar.Tests;

public class TrackedAppTests
{
    [Fact]
    public void TrackedApp_DefaultValues_AreCorrect()
    {
        var app = new TrackedApp { DisplayName = "Test", ProcessName = "test" };

        Assert.True(app.Enabled);
        Assert.Equal(LaunchStrategy.LaunchOnce, app.Launch);
        Assert.Null(app.ExecutablePath);
    }

    [Fact]
    public void TrackedApp_SerializesAndDeserializes()
    {
        var app = new TrackedApp
        {
            DisplayName = "Google Chrome",
            ProcessName = "chrome",
            ExecutablePath = null,
            Enabled = true,
            Launch = LaunchStrategy.LaunchOnce
        };

        var json = JsonSerializer.Serialize(app);
        var deserialized = JsonSerializer.Deserialize<TrackedApp>(json);

        Assert.NotNull(deserialized);
        Assert.Equal("Google Chrome", deserialized.DisplayName);
        Assert.Equal("chrome", deserialized.ProcessName);
        Assert.True(deserialized.Enabled);
        Assert.Equal(LaunchStrategy.LaunchOnce, deserialized.Launch);
    }

    [Fact]
    public void TrackedApp_LaunchPerWindow_Serializes()
    {
        var app = new TrackedApp
        {
            DisplayName = "Command Prompt",
            ProcessName = "cmd",
            ExecutablePath = "cmd.exe",
            Launch = LaunchStrategy.LaunchPerWindow
        };

        var json = JsonSerializer.Serialize(app);
        Assert.Contains("LaunchPerWindow", json);

        var deserialized = JsonSerializer.Deserialize<TrackedApp>(json);
        Assert.NotNull(deserialized);
        Assert.Equal(LaunchStrategy.LaunchPerWindow, deserialized.Launch);
        Assert.Equal("cmd.exe", deserialized.ExecutablePath);
    }

    [Fact]
    public void DiscoveredApp_ToTrackedApp_SetsCorrectDefaults()
    {
        var discovered = new DiscoveredApp
        {
            DisplayName = "Spotify",
            ProcessName = "Spotify",
            ExecutablePath = @"C:\Users\Test\AppData\Roaming\Spotify\Spotify.exe",
            Source = AppSource.RunningProcess
        };

        var tracked = discovered.ToTrackedApp();

        Assert.Equal("Spotify", tracked.DisplayName);
        Assert.Equal("Spotify", tracked.ProcessName);
        Assert.Equal(discovered.ExecutablePath, tracked.ExecutablePath);
        Assert.True(tracked.Enabled);
        Assert.Equal(LaunchStrategy.LaunchOnce, tracked.Launch);
    }

    [Fact]
    public void AppConfig_DefaultTrackedApps_ContainsExpectedApps()
    {
        var defaults = AppConfig.DefaultTrackedApps();

        Assert.Equal(6, defaults.Count);
        Assert.Contains(defaults, a => a.ProcessName == "chrome");
        Assert.Contains(defaults, a => a.ProcessName == "Code");
        Assert.Contains(defaults, a => a.ProcessName == "cmd");
        Assert.Contains(defaults, a => a.ProcessName == "powershell");
        Assert.Contains(defaults, a => a.ProcessName == "pwsh");
        Assert.Contains(defaults, a => a.ProcessName == "explorer");
    }
}
