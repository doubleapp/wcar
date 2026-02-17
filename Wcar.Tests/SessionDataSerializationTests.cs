using System.Text.Json;
using Wcar.Session;

namespace Wcar.Tests;

public class SessionDataSerializationTests
{
    [Fact]
    public void SessionSnapshot_SerializesAndDeserializes()
    {
        var snapshot = new SessionSnapshot
        {
            CapturedAt = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            DockerDesktopRunning = true,
            Windows = new List<WindowInfo>
            {
                new()
                {
                    ProcessName = "chrome",
                    Title = "Google",
                    Left = 100, Top = 200, Width = 800, Height = 600,
                    ShowCmd = 1
                }
            }
        };

        var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
        var deserialized = JsonSerializer.Deserialize<SessionSnapshot>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(snapshot.CapturedAt, deserialized.CapturedAt);
        Assert.True(deserialized.DockerDesktopRunning);
        Assert.Single(deserialized.Windows);
    }

    [Fact]
    public void WindowInfo_PreservesAllFields()
    {
        var win = new WindowInfo
        {
            ProcessName = "cmd",
            Title = "Command Prompt",
            Left = 10, Top = 20, Width = 300, Height = 400,
            ShowCmd = 3,
            WorkingDirectory = @"C:\Users\Test",
            FolderPath = null
        };

        var json = JsonSerializer.Serialize(win);
        var deserialized = JsonSerializer.Deserialize<WindowInfo>(json);

        Assert.NotNull(deserialized);
        Assert.Equal("cmd", deserialized.ProcessName);
        Assert.Equal(@"C:\Users\Test", deserialized.WorkingDirectory);
        Assert.Null(deserialized.FolderPath);
    }

    [Fact]
    public void SessionSnapshot_EmptyWindowsList_Serializes()
    {
        var snapshot = new SessionSnapshot();

        var json = JsonSerializer.Serialize(snapshot);
        var deserialized = JsonSerializer.Deserialize<SessionSnapshot>(json);

        Assert.NotNull(deserialized);
        Assert.Empty(deserialized.Windows);
        Assert.False(deserialized.DockerDesktopRunning);
    }

    [Fact]
    public void WindowInfo_ExplorerWithFolderPath_Serializes()
    {
        var win = new WindowInfo
        {
            ProcessName = "explorer",
            Title = "Documents",
            Left = 0, Top = 0, Width = 1024, Height = 768,
            ShowCmd = 1,
            FolderPath = @"C:\Users\Test\Documents"
        };

        var json = JsonSerializer.Serialize(win);
        Assert.Contains("Documents", json);
        Assert.Contains(@"C:\\Users\\Test\\Documents", json);
    }

    [Fact]
    public void MonitorInfo_RoundTrips_Serialization()
    {
        var snapshot = new SessionSnapshot
        {
            Monitors = new List<MonitorInfo>
            {
                new() { DeviceName = "\\\\.\\DISPLAY1", Left = 0, Top = 0, Width = 1920, Height = 1080, IsPrimary = true },
                new() { DeviceName = "\\\\.\\DISPLAY2", Left = 1920, Top = 0, Width = 2560, Height = 1440 }
            }
        };

        var json = JsonSerializer.Serialize(snapshot);
        var deserialized = JsonSerializer.Deserialize<SessionSnapshot>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(2, deserialized.Monitors.Count);
        Assert.Equal(1920, deserialized.Monitors[0].Width);
        Assert.True(deserialized.Monitors[0].IsPrimary);
        Assert.Equal(2560, deserialized.Monitors[1].Width);
    }

    [Fact]
    public void SessionSnapshot_NoMonitors_BackwardCompat()
    {
        // Old session.json without Monitors field should load with empty list
        var oldJson = """
        {
            "CapturedAt": "2025-01-15T10:30:00Z",
            "Windows": [],
            "DockerDesktopRunning": false
        }
        """;

        var deserialized = JsonSerializer.Deserialize<SessionSnapshot>(oldJson);

        Assert.NotNull(deserialized);
        Assert.Empty(deserialized.Monitors);
        Assert.Empty(deserialized.Windows);
    }

    [Fact]
    public void SessionSnapshot_MultipleWindows_PreservesOrder()
    {
        var snapshot = new SessionSnapshot
        {
            Windows = new List<WindowInfo>
            {
                new() { ProcessName = "chrome", Title = "Tab 1" },
                new() { ProcessName = "cmd", Title = "Terminal" },
                new() { ProcessName = "explorer", Title = "Files" }
            }
        };

        var json = JsonSerializer.Serialize(snapshot);
        var deserialized = JsonSerializer.Deserialize<SessionSnapshot>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(3, deserialized.Windows.Count);
        Assert.Equal("chrome", deserialized.Windows[0].ProcessName);
        Assert.Equal("cmd", deserialized.Windows[1].ProcessName);
        Assert.Equal("explorer", deserialized.Windows[2].ProcessName);
    }
}
