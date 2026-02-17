namespace Wcar.Session;

/// <summary>
/// Handles the auto-map algorithm and proportional position translation
/// when restoring windows across different monitor configurations.
/// </summary>
public static class ScreenMapper
{
    /// <summary>
    /// Auto-maps saved monitors to current monitors based on position proximity.
    /// Returns an array where result[savedIndex] = currentIndex.
    /// </summary>
    public static int[] AutoMap(IReadOnlyList<MonitorInfo> saved, IReadOnlyList<MonitorInfo> current)
    {
        if (saved.Count == 0) return Array.Empty<int>();
        if (current.Count == 0) return Enumerable.Repeat(0, saved.Count).ToArray();

        var result = new int[saved.Count];

        for (int s = 0; s < saved.Count; s++)
        {
            result[s] = FindBestMatch(saved[s], current);
        }

        return result;
    }

    /// <summary>
    /// Finds the index of the current monitor that best matches the saved monitor
    /// by Euclidean distance of their top-left corners.
    /// </summary>
    private static int FindBestMatch(MonitorInfo saved, IReadOnlyList<MonitorInfo> current)
    {
        int bestIndex = 0;
        double bestDist = double.MaxValue;

        for (int i = 0; i < current.Count; i++)
        {
            var dx = saved.Left - current[i].Left;
            var dy = saved.Top - current[i].Top;
            var dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestIndex = i;
            }
        }

        // Fallback: if all distances are enormous, prefer primary
        if (bestDist > 10000)
        {
            for (int i = 0; i < current.Count; i++)
            {
                if (current[i].IsPrimary) return i;
            }
        }

        return bestIndex;
    }

    /// <summary>
    /// Translates a window's position from saved monitor coordinates to current monitor coordinates
    /// using proportional mapping. Maximized windows (ShowCmd == 3) are left with their
    /// top-left corner translated but clamped — the caller should keep ShowCmd = 3.
    /// </summary>
    public static (int Left, int Top, int Width, int Height) TranslatePosition(
        int winLeft, int winTop, int winWidth, int winHeight,
        MonitorInfo savedMonitor, MonitorInfo currentMonitor)
    {
        // Guard against zero-size monitors
        if (savedMonitor.Width <= 0 || savedMonitor.Height <= 0)
            return (currentMonitor.Left, currentMonitor.Top, winWidth, winHeight);

        var relX = (double)(winLeft - savedMonitor.Left) / savedMonitor.Width;
        var relY = (double)(winTop - savedMonitor.Top) / savedMonitor.Height;
        var relW = (double)winWidth / savedMonitor.Width;
        var relH = (double)winHeight / savedMonitor.Height;

        var newLeft = (int)(currentMonitor.Left + relX * currentMonitor.Width);
        var newTop = (int)(currentMonitor.Top + relY * currentMonitor.Height);
        var newWidth = Math.Max(50, (int)(relW * currentMonitor.Width));
        var newHeight = Math.Max(50, (int)(relH * currentMonitor.Height));

        // Clamp to current monitor bounds
        newLeft = Math.Max(currentMonitor.Left,
                    Math.Min(newLeft, currentMonitor.Left + currentMonitor.Width - newWidth));
        newTop = Math.Max(currentMonitor.Top,
                   Math.Min(newTop, currentMonitor.Top + currentMonitor.Height - newHeight));

        return (newLeft, newTop, newWidth, newHeight);
    }
}
