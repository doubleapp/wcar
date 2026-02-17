using Wcar.Session;

namespace Wcar.Tests;

public class MonitorHelperTests
{
    private static MonitorInfo MakeMonitor(int left, int top, int width, int height, bool primary = false) =>
        new() { DeviceName = $"\\\\.\\ DISPLAY", Left = left, Top = top, Width = width, Height = height, IsPrimary = primary };

    [Fact]
    public void AreConfigurationsEqual_SameMonitors_ReturnsTrue()
    {
        var saved = new[] { MakeMonitor(0, 0, 1920, 1080, primary: true) };
        var current = new[] { MakeMonitor(0, 0, 1920, 1080, primary: true) };

        Assert.True(MonitorHelper.AreConfigurationsEqual(saved, current));
    }

    [Fact]
    public void AreConfigurationsEqual_DifferentCount_ReturnsFalse()
    {
        var saved = new[] { MakeMonitor(0, 0, 1920, 1080), MakeMonitor(1920, 0, 1920, 1080) };
        var current = new[] { MakeMonitor(0, 0, 1920, 1080) };

        Assert.False(MonitorHelper.AreConfigurationsEqual(saved, current));
    }

    [Fact]
    public void AreConfigurationsEqual_BoundsWithinTolerance_ReturnsTrue()
    {
        var saved = new[] { MakeMonitor(0, 0, 1920, 1080, primary: true) };
        var current = new[] { MakeMonitor(5, 5, 1925, 1085, primary: true) }; // within 10px tolerance

        Assert.True(MonitorHelper.AreConfigurationsEqual(saved, current));
    }

    [Fact]
    public void AreConfigurationsEqual_BoundsOutsideTolerance_ReturnsFalse()
    {
        var saved = new[] { MakeMonitor(0, 0, 1920, 1080, primary: true) };
        var current = new[] { MakeMonitor(100, 0, 1920, 1080, primary: true) }; // 100px difference

        Assert.False(MonitorHelper.AreConfigurationsEqual(saved, current));
    }

    [Fact]
    public void AssignMonitorIndex_WindowCenterOnFirstMonitor_ReturnsZero()
    {
        var monitors = new[]
        {
            MakeMonitor(0, 0, 1920, 1080, primary: true),
            MakeMonitor(1920, 0, 1920, 1080)
        };

        // Window center at (500, 500) — on monitor 0
        var index = MonitorHelper.AssignMonitorIndex(100, 100, 800, 800, monitors);

        Assert.Equal(0, index);
    }

    [Fact]
    public void AssignMonitorIndex_WindowCenterOnSecondMonitor_ReturnsOne()
    {
        var monitors = new[]
        {
            MakeMonitor(0, 0, 1920, 1080, primary: true),
            MakeMonitor(1920, 0, 1920, 1080)
        };

        // Window center at (2320, 540) — on monitor 1
        var index = MonitorHelper.AssignMonitorIndex(1920, 100, 800, 880, monitors);

        Assert.Equal(1, index);
    }

    [Fact]
    public void AssignMonitorIndex_NullMonitors_ReturnsFallback()
    {
        var monitors = Array.Empty<MonitorInfo>();

        // Empty monitors — should not throw
        var index = MonitorHelper.AssignMonitorIndex(0, 0, 100, 100, monitors);

        Assert.Equal(0, index);
    }

    [Fact]
    public void AssignMonitorIndex_FallsBackToPrimary_WhenNoneMatch()
    {
        var monitors = new[]
        {
            MakeMonitor(0, 0, 1920, 1080, primary: false),
            MakeMonitor(1920, 0, 1920, 1080, primary: true)
        };

        // Window at a position not on any monitor
        var index = MonitorHelper.AssignMonitorIndex(5000, 5000, 100, 100, monitors);

        Assert.Equal(1, index); // primary monitor
    }
}
