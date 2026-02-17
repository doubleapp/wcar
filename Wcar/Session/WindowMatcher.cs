namespace Wcar.Session;

/// <summary>
/// Matches saved WindowInfo entries to actual window handles for LaunchOnce apps.
/// Uses title-based matching with index-order fallback.
/// </summary>
public static class WindowMatcher
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan StabilityThreshold = TimeSpan.FromMilliseconds(1000); // 2 polls
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Pure matching: matches saved windows to actual windows by title, with index fallback.
    /// Returns a list of (savedIndex, actualHandle) pairs for matched windows.
    /// </summary>
    public static List<(int SavedIndex, IntPtr ActualHandle)> Match(
        IReadOnlyList<WindowInfo> saved,
        IReadOnlyList<(IntPtr Handle, string Title)> actual)
    {
        var result = new List<(int, IntPtr)>();
        var usedActual = new HashSet<int>();

        // Pass 1: title-based matching
        for (int s = 0; s < saved.Count; s++)
        {
            var savedTitle = saved[s].Title;
            if (string.IsNullOrEmpty(savedTitle)) continue;

            for (int a = 0; a < actual.Count; a++)
            {
                if (usedActual.Contains(a)) continue;

                if (actual[a].Title.Contains(savedTitle, StringComparison.OrdinalIgnoreCase) ||
                    savedTitle.Contains(actual[a].Title, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add((s, actual[a].Handle));
                    usedActual.Add(a);
                    break;
                }
            }
        }

        // Pass 2: index-order fallback for unmatched saved entries
        var matchedSavedIndices = new HashSet<int>(result.Select(r => r.Item1));
        int nextActual = 0;

        for (int s = 0; s < saved.Count; s++)
        {
            if (matchedSavedIndices.Contains(s)) continue;

            // Find the next unused actual window
            while (nextActual < actual.Count && usedActual.Contains(nextActual))
                nextActual++;

            if (nextActual < actual.Count)
            {
                result.Add((s, actual[nextActual].Handle));
                usedActual.Add(nextActual);
                nextActual++;
            }
        }

        return result;
    }

    /// <summary>
    /// Polls until windows for a process stabilize (count unchanged for 2 polls) or timeout.
    /// Returns the stable list of (handle, title) pairs.
    /// </summary>
    public static List<(IntPtr Handle, string Title)> WaitForStableWindows(
        string processName, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? Timeout);
        var prevCount = -1;
        var stableStart = DateTime.MinValue;

        while (DateTime.UtcNow < deadline)
        {
            var current = GetWindowsForProcess(processName);

            if (current.Count == prevCount)
            {
                if (stableStart == DateTime.MinValue)
                    stableStart = DateTime.UtcNow;
                else if (DateTime.UtcNow - stableStart >= StabilityThreshold)
                    return current;
            }
            else
            {
                prevCount = current.Count;
                stableStart = DateTime.MinValue;
            }

            Thread.Sleep(PollInterval);
        }

        return GetWindowsForProcess(processName);
    }

    private static List<(IntPtr Handle, string Title)> GetWindowsForProcess(string processName)
    {
        var result = new List<(IntPtr, string)>();
        var pids = new HashSet<uint>();

        try
        {
            var procs = System.Diagnostics.Process.GetProcessesByName(processName);
            foreach (var p in procs)
            {
                pids.Add((uint)p.Id);
                p.Dispose();
            }
        }
        catch { return result; }

        Interop.NativeMethods.EnumWindows((hWnd, _) =>
        {
            try
            {
                if (!Interop.NativeMethods.IsWindowVisible(hWnd)) return true;

                Interop.NativeMethods.GetWindowThreadProcessId(hWnd, out uint pid);
                if (!pids.Contains(pid)) return true;

                var title = GetWindowTitle(hWnd);
                if (!string.IsNullOrEmpty(title))
                    result.Add((hWnd, title));
            }
            catch { }
            return true;
        }, IntPtr.Zero);

        return result;
    }

    private static string GetWindowTitle(IntPtr hWnd)
    {
        int length = Interop.NativeMethods.GetWindowTextLength(hWnd);
        if (length == 0) return string.Empty;
        var buffer = new char[length + 1];
        Interop.NativeMethods.GetWindowText(hWnd, buffer, buffer.Length);
        return new string(buffer, 0, length);
    }
}
