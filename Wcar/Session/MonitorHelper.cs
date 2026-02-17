namespace Wcar.Session;

public interface IMonitorProvider
{
    MonitorInfo[] GetMonitors();
}

public class MonitorProvider : IMonitorProvider
{
    public MonitorInfo[] GetMonitors()
    {
        return Screen.AllScreens.Select((s, i) => new MonitorInfo
        {
            DeviceName = s.DeviceName,
            Left = s.Bounds.Left,
            Top = s.Bounds.Top,
            Width = s.Bounds.Width,
            Height = s.Bounds.Height,
            IsPrimary = s.Primary
        }).ToArray();
    }
}

public static class MonitorHelper
{
    private const int BoundsTolerance = 10; // pixels

    /// <summary>
    /// Compares two monitor configurations for equality within tolerance.
    /// Returns true if they appear to be the same configuration.
    /// </summary>
    public static bool AreConfigurationsEqual(IReadOnlyList<MonitorInfo> saved, IReadOnlyList<MonitorInfo> current)
    {
        if (saved.Count != current.Count)
            return false;

        // Sort both by position for comparison (top-left corner)
        var sortedSaved = saved.OrderBy(m => m.Left).ThenBy(m => m.Top).ToList();
        var sortedCurrent = current.OrderBy(m => m.Left).ThenBy(m => m.Top).ToList();

        for (int i = 0; i < sortedSaved.Count; i++)
        {
            var s = sortedSaved[i];
            var c = sortedCurrent[i];

            if (Math.Abs(s.Left - c.Left) > BoundsTolerance) return false;
            if (Math.Abs(s.Top - c.Top) > BoundsTolerance) return false;
            if (Math.Abs(s.Width - c.Width) > BoundsTolerance) return false;
            if (Math.Abs(s.Height - c.Height) > BoundsTolerance) return false;
        }

        return true;
    }

    /// <summary>
    /// Assigns a MonitorIndex to a window based on which monitor contains its center point.
    /// Falls back to the primary monitor (index 0) if no match is found.
    /// </summary>
    public static int AssignMonitorIndex(int windowLeft, int windowTop, int windowWidth, int windowHeight,
        IReadOnlyList<MonitorInfo> monitors)
    {
        var centerX = windowLeft + windowWidth / 2;
        var centerY = windowTop + windowHeight / 2;

        for (int i = 0; i < monitors.Count; i++)
        {
            var m = monitors[i];
            if (centerX >= m.Left && centerX < m.Left + m.Width &&
                centerY >= m.Top && centerY < m.Top + m.Height)
            {
                return i;
            }
        }

        // Fallback: return index of primary monitor
        for (int i = 0; i < monitors.Count; i++)
        {
            if (monitors[i].IsPrimary) return i;
        }

        return 0;
    }
}
