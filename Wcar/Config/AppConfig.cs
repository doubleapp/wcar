namespace Wcar.Config;

public class AppConfig
{
    public int AutoSaveIntervalMinutes { get; set; } = 5;
    public bool AutoSaveEnabled { get; set; } = true;
    public List<ScriptEntry> Scripts { get; set; } = new();

    public Dictionary<string, bool> TrackedApps { get; set; } = new()
    {
        ["Chrome"] = true,
        ["VSCode"] = true,
        ["CMD"] = true,
        ["PowerShell"] = true,
        ["Explorer"] = true,
        ["DockerDesktop"] = true
    };

    public bool AutoStartEnabled { get; set; }
    public bool DiskCheckEnabled { get; set; }
    public bool AutoRestoreEnabled { get; set; }
}
