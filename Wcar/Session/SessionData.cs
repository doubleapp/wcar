namespace Wcar.Session;

public class MonitorInfo
{
    public string DeviceName { get; set; } = string.Empty;
    public int Left { get; set; }
    public int Top { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public bool IsPrimary { get; set; }
}

public class SessionSnapshot
{
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
    public List<WindowInfo> Windows { get; set; } = new();
    public List<MonitorInfo> Monitors { get; set; } = new();
    public bool DockerDesktopRunning { get; set; }  // Deprecated, kept for backward compat
}

public class WindowInfo
{
    public string ProcessName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Left { get; set; }
    public int Top { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int ShowCmd { get; set; }
    public string? WorkingDirectory { get; set; }
    public string? FolderPath { get; set; }
    public int MonitorIndex { get; set; }   // Index into SessionSnapshot.Monitors
    public int ZOrder { get; set; }         // 0 = topmost, increments downward
}
