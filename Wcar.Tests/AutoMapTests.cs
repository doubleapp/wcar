using Wcar.Session;

namespace Wcar.Tests;

public class AutoMapTests
{
    private static MonitorInfo MakeMonitor(int left, int top, int width = 1920, int height = 1080, bool primary = false) =>
        new() { DeviceName = "TEST", Left = left, Top = top, Width = width, Height = height, IsPrimary = primary };

    [Fact]
    public void AutoMap_SameCount_MapsByPosition()
    {
        var saved = new[] { MakeMonitor(0, 0), MakeMonitor(1920, 0) };
        var current = new[] { MakeMonitor(0, 0), MakeMonitor(1920, 0) };

        var map = ScreenMapper.AutoMap(saved, current);

        Assert.Equal(2, map.Length);
        Assert.Equal(0, map[0]); // saved[0] → current[0]
        Assert.Equal(1, map[1]); // saved[1] → current[1]
    }

    [Fact]
    public void AutoMap_FewerCurrentMonitors_ConsolidatesToNearest()
    {
        var saved = new[] { MakeMonitor(0, 0), MakeMonitor(1920, 0), MakeMonitor(3840, 0) };
        var current = new[] { MakeMonitor(0, 0, primary: true), MakeMonitor(1920, 0) };

        var map = ScreenMapper.AutoMap(saved, current);

        // saved[0] (left=0) → current[0] (left=0)
        Assert.Equal(0, map[0]);
        // saved[1] (left=1920) → current[1] (left=1920)
        Assert.Equal(1, map[1]);
        // saved[2] (left=3840) → current[1] (left=1920, nearest)
        Assert.Equal(1, map[2]);
    }

    [Fact]
    public void AutoMap_EmptySaved_ReturnsEmpty()
    {
        var saved = Array.Empty<MonitorInfo>();
        var current = new[] { MakeMonitor(0, 0) };

        var map = ScreenMapper.AutoMap(saved, current);

        Assert.Empty(map);
    }

    [Fact]
    public void AutoMap_EmptyCurrent_ReturnsZeros()
    {
        var saved = new[] { MakeMonitor(0, 0), MakeMonitor(1920, 0) };
        var current = Array.Empty<MonitorInfo>();

        var map = ScreenMapper.AutoMap(saved, current);

        Assert.All(map, m => Assert.Equal(0, m));
    }

    [Fact]
    public void TranslatePosition_SameMonitor_PreservesRelativePosition()
    {
        var monitor = MakeMonitor(0, 0, 1920, 1080);

        var (left, top, width, height) = ScreenMapper.TranslatePosition(
            100, 200, 800, 600, monitor, monitor);

        Assert.Equal(100, left);
        Assert.Equal(200, top);
        Assert.Equal(800, width);
        Assert.Equal(600, height);
    }

    [Fact]
    public void TranslatePosition_4KTo1080p_ScalesProportionally()
    {
        var saved4K = MakeMonitor(0, 0, 3840, 2160);
        var current1080 = MakeMonitor(0, 0, 1920, 1080);

        // Window at (0, 0) with full 4K size
        var (left, top, width, height) = ScreenMapper.TranslatePosition(
            0, 0, 3840, 2160, saved4K, current1080);

        Assert.Equal(0, left);
        Assert.Equal(0, top);
        Assert.Equal(1920, width);
        Assert.Equal(1080, height);
    }

    [Fact]
    public void TranslatePosition_ClampsToMonitorBounds()
    {
        var savedMonitor = MakeMonitor(0, 0, 1920, 1080);
        var currentMonitor = MakeMonitor(0, 0, 800, 600);

        // Window positioned off-screen relative to current monitor
        var (left, top, width, height) = ScreenMapper.TranslatePosition(
            1800, 900, 300, 300, savedMonitor, currentMonitor);

        // Should be clamped within current monitor
        Assert.True(left >= currentMonitor.Left);
        Assert.True(top >= currentMonitor.Top);
        Assert.True(left + width <= currentMonitor.Left + currentMonitor.Width);
        Assert.True(top + height <= currentMonitor.Top + currentMonitor.Height);
    }
}
