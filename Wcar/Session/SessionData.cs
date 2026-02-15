namespace Wcar.Session;

public class SessionSnapshot
{
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
    public List<WindowInfo> Windows { get; set; } = new();
    public bool DockerDesktopRunning { get; set; }
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
}
