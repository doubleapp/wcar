namespace Wcar.Config;

public class AppConfig
{
    public int AutoSaveIntervalMinutes { get; set; } = 5;
    public bool AutoSaveEnabled { get; set; } = true;
    public List<ScriptEntry> Scripts { get; set; } = new();

    public List<TrackedApp> TrackedApps { get; set; } = DefaultTrackedApps();

    public bool AutoStartEnabled { get; set; }
    public bool AutoRestoreEnabled { get; set; }

    public static List<TrackedApp> DefaultTrackedApps() => new()
    {
        new() { DisplayName = "Google Chrome",      ProcessName = "chrome",      ExecutablePath = null,            Launch = LaunchStrategy.LaunchOnce,      Enabled = true },
        new() { DisplayName = "Visual Studio Code", ProcessName = "Code",        ExecutablePath = null,            Launch = LaunchStrategy.LaunchOnce,      Enabled = true },
        new() { DisplayName = "Command Prompt",     ProcessName = "cmd",         ExecutablePath = "cmd.exe",       Launch = LaunchStrategy.LaunchPerWindow, Enabled = true },
        new() { DisplayName = "PowerShell",         ProcessName = "powershell",  ExecutablePath = "powershell.exe",Launch = LaunchStrategy.LaunchPerWindow, Enabled = true },
        new() { DisplayName = "PowerShell Core",    ProcessName = "pwsh",        ExecutablePath = "pwsh.exe",      Launch = LaunchStrategy.LaunchPerWindow, Enabled = true },
        new() { DisplayName = "File Explorer",      ProcessName = "explorer",    ExecutablePath = "explorer.exe",  Launch = LaunchStrategy.LaunchPerWindow, Enabled = true },
    };
}
