using System.Text.Json.Serialization;

namespace Wcar.Config;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LaunchStrategy
{
    LaunchOnce,      // App is started once; it manages its own windows (Chrome, VSCode, Slack)
    LaunchPerWindow  // One process started per saved window (CMD, PowerShell, Explorer)
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AppSource
{
    StartMenu,
    RunningProcess
}

public class TrackedApp
{
    public string DisplayName { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public string? ExecutablePath { get; set; }
    public bool Enabled { get; set; } = true;
    public LaunchStrategy Launch { get; set; } = LaunchStrategy.LaunchOnce;
}

public class DiscoveredApp
{
    public string DisplayName { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public string? ExecutablePath { get; set; }
    public AppSource Source { get; set; }

    public TrackedApp ToTrackedApp() => new()
    {
        DisplayName = DisplayName,
        ProcessName = ProcessName,
        ExecutablePath = ExecutablePath,
        Enabled = true,
        Launch = LaunchStrategy.LaunchOnce
    };
}
