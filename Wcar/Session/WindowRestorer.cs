using System.Diagnostics;
using Wcar.Config;
using Wcar.Interop;

namespace Wcar.Session;

public interface IProcessLauncher
{
    Process? Start(ProcessStartInfo psi);
}

public class DefaultProcessLauncher : IProcessLauncher
{
    public Process? Start(ProcessStartInfo psi) => Process.Start(psi);
}

public class RestoreResult
{
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class WindowRestorer
{
    private readonly List<TrackedApp> _trackedApps;
    private readonly IProcessLauncher _launcher;

    private static readonly HashSet<string> CmdProcessNames = new(StringComparer.OrdinalIgnoreCase) { "cmd" };
    private static readonly HashSet<string> PsProcessNames = new(StringComparer.OrdinalIgnoreCase) { "powershell", "pwsh" };
    private static readonly HashSet<string> ExplorerProcessNames = new(StringComparer.OrdinalIgnoreCase) { "explorer" };

    public WindowRestorer(List<TrackedApp> trackedApps, IProcessLauncher? launcher = null)
    {
        _trackedApps = trackedApps;
        _launcher = launcher ?? new DefaultProcessLauncher();
    }

    public RestoreResult Restore(SessionSnapshot snapshot)
    {
        var result = new RestoreResult();
        // (hwnd, zorder) for z-order restoration pass
        var restoredWindows = new List<(IntPtr Handle, int ZOrder)>();

        // Group windows by process name for LaunchOnce handling
        var launchedOnce = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var win in snapshot.Windows)
        {
            try
            {
                var trackedApp = FindTrackedApp(win.ProcessName);
                if (trackedApp == null)
                {
                    result.Errors.Add($"Cannot determine how to launch {win.ProcessName}");
                    continue;
                }

                if (trackedApp.Launch == LaunchStrategy.LaunchOnce)
                {
                    // Launch once; window matching happens after all launches
                    if (!launchedOnce.Contains(win.ProcessName))
                    {
                        LaunchApp(trackedApp, win, result);
                        launchedOnce.Add(win.ProcessName);
                    }
                }
                else
                {
                    // LaunchPerWindow: launch once per saved window, get handle, position
                    var hwnd = LaunchAndGetHandle(trackedApp, win, result);
                    if (hwnd != IntPtr.Zero)
                    {
                        SetPlacement(hwnd, win);
                        restoredWindows.Add((hwnd, win.ZOrder));
                    }
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Failed to restore {win.ProcessName}: {ex.Message}");
            }
        }

        // For LaunchOnce apps: wait for windows to stabilize, then match + position
        var launchOnceGroups = snapshot.Windows
            .Where(w => FindTrackedApp(w.ProcessName)?.Launch == LaunchStrategy.LaunchOnce)
            .GroupBy(w => w.ProcessName, StringComparer.OrdinalIgnoreCase);

        foreach (var group in launchOnceGroups)
        {
            try
            {
                var processName = group.Key;
                var savedWindows = group.ToList();

                var actual = WindowMatcher.WaitForStableWindows(processName, TimeSpan.FromSeconds(15));
                var matches = WindowMatcher.Match(savedWindows, actual);

                foreach (var (savedIdx, handle) in matches)
                {
                    try
                    {
                        SetPlacement(handle, savedWindows[savedIdx]);
                        restoredWindows.Add((handle, savedWindows[savedIdx].ZOrder));
                    }
                    catch (Exception ex)
                    {
                        result.Warnings.Add($"Could not position {processName} window: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"Window matching failed for {group.Key}: {ex.Message}");
            }
        }

        // Z-order restoration: bottom-first → topmost last
        try
        {
            RestoreZOrder(restoredWindows);
        }
        catch (Exception ex)
        {
            result.Warnings.Add($"Z-order restoration failed: {ex.Message}");
        }

        return result;
    }

    private void LaunchApp(TrackedApp app, WindowInfo win, RestoreResult result)
    {
        var psi = BuildProcessStartInfo(app, win);
        if (psi == null)
        {
            result.Errors.Add($"Cannot determine how to launch {app.ProcessName}");
            return;
        }

        try
        {
            _launcher.Start(psi);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to start {app.ProcessName}: {ex.Message}");
        }
    }

    private IntPtr LaunchAndGetHandle(TrackedApp app, WindowInfo win, RestoreResult result)
    {
        var psi = BuildProcessStartInfo(app, win);
        if (psi == null)
        {
            result.Errors.Add($"Cannot determine how to launch {app.ProcessName}");
            return IntPtr.Zero;
        }

        Process? proc;
        try
        {
            proc = _launcher.Start(psi);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to start {app.ProcessName}: {ex.Message}");
            return IntPtr.Zero;
        }

        if (proc == null) return IntPtr.Zero;

        var hwnd = WaitForMainWindow(proc, TimeSpan.FromSeconds(5));
        return hwnd;
    }

    private TrackedApp? FindTrackedApp(string processName)
    {
        return _trackedApps.FirstOrDefault(
            a => a.Enabled && a.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase));
    }

    internal ProcessStartInfo? BuildProcessStartInfo(TrackedApp app, WindowInfo win)
    {
        var name = app.ProcessName.ToLowerInvariant();

        // CMD: special CWD handling
        if (CmdProcessNames.Contains(name))
        {
            var cwd = win.WorkingDirectory ?? @"C:\";
            return new ProcessStartInfo("cmd.exe", $"/K cd /d \"{cwd}\"") { UseShellExecute = true };
        }

        // PowerShell/pwsh: special CWD handling
        if (PsProcessNames.Contains(name))
        {
            var cwd = win.WorkingDirectory ?? @"C:\";
            var exe = app.ExecutablePath ?? (name + ".exe");
            return new ProcessStartInfo(exe, $"-NoExit -Command \"Set-Location '{cwd}'\"") { UseShellExecute = true };
        }

        // Explorer: special folder path handling
        if (ExplorerProcessNames.Contains(name))
        {
            var folder = win.FolderPath;
            var exe = app.ExecutablePath ?? "explorer.exe";
            return folder != null
                ? new ProcessStartInfo(exe, $"\"{folder}\"") { UseShellExecute = true }
                : new ProcessStartInfo(exe) { UseShellExecute = true };
        }

        // Generic: use executable path or process name with UseShellExecute
        var target = app.ExecutablePath ?? app.ProcessName;
        return new ProcessStartInfo(target) { UseShellExecute = true };
    }

    private static IntPtr WaitForMainWindow(Process proc, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            proc.Refresh();
            if (proc.MainWindowHandle != IntPtr.Zero)
                return proc.MainWindowHandle;
            Thread.Sleep(100);
        }
        return IntPtr.Zero;
    }

    internal static void SetPlacement(IntPtr hwnd, WindowInfo win)
    {
        var wp = WINDOWPLACEMENT.Default;
        wp.ShowCmd = win.ShowCmd;
        wp.NormalPosition = new RECT
        {
            Left = win.Left,
            Top = win.Top,
            Right = win.Left + win.Width,
            Bottom = win.Top + win.Height
        };

        // Clamp to primary monitor if off-screen
        var screen = Screen.PrimaryScreen;
        if (screen != null && win.ShowCmd != 3) // Don't clamp maximized windows
        {
            var bounds = screen.WorkingArea;
            if (wp.NormalPosition.Left > bounds.Right ||
                wp.NormalPosition.Right < bounds.Left ||
                wp.NormalPosition.Top > bounds.Bottom ||
                wp.NormalPosition.Bottom < bounds.Top)
            {
                var w = wp.NormalPosition.Width;
                var h = wp.NormalPosition.Height;
                wp.NormalPosition.Left = bounds.Left + (bounds.Width - w) / 2;
                wp.NormalPosition.Top = bounds.Top + (bounds.Height - h) / 2;
                wp.NormalPosition.Right = wp.NormalPosition.Left + w;
                wp.NormalPosition.Bottom = wp.NormalPosition.Top + h;
            }
        }

        NativeMethods.SetWindowPlacement(hwnd, ref wp);
    }

    private static void RestoreZOrder(List<(IntPtr Handle, int ZOrder)> windows)
    {
        // Sort descending by ZOrder (bottom-first → topmost last)
        // The last window processed (ZOrder=0) ends up on top
        const uint SWP_NOSIZE = 0x0001;
        const uint SWP_NOMOVE = 0x0002;
        const uint SWP_NOACTIVATE = 0x0010;
        var HWND_TOP = IntPtr.Zero;

        foreach (var (handle, _) in windows.OrderByDescending(w => w.ZOrder))
        {
            try
            {
                NativeMethods.SetWindowPos(handle, HWND_TOP, 0, 0, 0, 0,
                    SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);
            }
            catch { }
        }
    }

    private static bool IsProcessRunning(string processName)
    {
        try
        {
            var procs = Process.GetProcessesByName(processName);
            var running = procs.Length > 0;
            foreach (var p in procs) p.Dispose();
            return running;
        }
        catch
        {
            return false;
        }
    }
}
