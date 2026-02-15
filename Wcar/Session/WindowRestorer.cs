using System.Diagnostics;
using Wcar.Interop;

namespace Wcar.Session;

public class RestoreResult
{
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class WindowRestorer
{
    private readonly Dictionary<string, bool> _trackedApps;

    private static readonly HashSet<string> ChromeProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "chrome"
    };

    private static readonly HashSet<string> VsCodeProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Code"
    };

    public WindowRestorer(Dictionary<string, bool> trackedApps)
    {
        _trackedApps = trackedApps;
    }

    public RestoreResult Restore(SessionSnapshot snapshot)
    {
        var result = new RestoreResult();
        var launchedDedup = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Docker first
        if (snapshot.DockerDesktopRunning && _trackedApps.GetValueOrDefault("DockerDesktop", false))
        {
            RestoreDocker(result);
        }

        foreach (var win in snapshot.Windows)
        {
            try
            {
                RestoreWindow(win, result, launchedDedup);
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Failed to restore {win.ProcessName}: {ex.Message}");
            }
        }

        return result;
    }

    private void RestoreWindow(WindowInfo win, RestoreResult result, HashSet<string> launchedDedup)
    {
        // Skip if already running for Chrome/VSCode (they manage their own windows)
        if (ChromeProcessNames.Contains(win.ProcessName) || VsCodeProcessNames.Contains(win.ProcessName))
        {
            if (IsProcessRunning(win.ProcessName))
            {
                result.Warnings.Add($"{win.ProcessName} is already running, skipping launch.");
                return;
            }

            if (!launchedDedup.Add(win.ProcessName))
                return; // Already launched this process in this restore
        }

        var psi = BuildProcessStartInfo(win);
        if (psi == null)
        {
            result.Errors.Add($"Cannot determine how to launch {win.ProcessName}");
            return;
        }

        Process? proc;
        try
        {
            proc = Process.Start(psi);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to start {win.ProcessName}: {ex.Message}");
            return;
        }

        if (proc == null) return;

        // Wait for window handle
        var hwnd = WaitForMainWindow(proc, timeout: TimeSpan.FromSeconds(5));
        if (hwnd == IntPtr.Zero)
            return;

        // Set window placement
        SetPlacement(hwnd, win);
    }

    private ProcessStartInfo? BuildProcessStartInfo(WindowInfo win)
    {
        var name = win.ProcessName.ToLowerInvariant();

        if (name == "chrome")
        {
            return new ProcessStartInfo("chrome") { UseShellExecute = true };
        }

        if (name == "code")
        {
            return new ProcessStartInfo("code") { UseShellExecute = true };
        }

        if (name == "cmd")
        {
            var cwd = win.WorkingDirectory ?? @"C:\";
            return new ProcessStartInfo("cmd.exe", $"/K cd /d \"{cwd}\"")
            {
                UseShellExecute = true
            };
        }

        if (name is "powershell" or "pwsh")
        {
            var cwd = win.WorkingDirectory ?? @"C:\";
            return new ProcessStartInfo(win.ProcessName + ".exe",
                $"-NoExit -Command \"Set-Location '{cwd}'\"")
            {
                UseShellExecute = true
            };
        }

        if (name == "explorer")
        {
            var folder = win.FolderPath;
            return folder != null
                ? new ProcessStartInfo("explorer.exe", $"\"{folder}\"") { UseShellExecute = true }
                : new ProcessStartInfo("explorer.exe") { UseShellExecute = true };
        }

        return null;
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

    private static void SetPlacement(IntPtr hwnd, WindowInfo win)
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
        if (screen != null)
        {
            var bounds = screen.WorkingArea;
            if (wp.NormalPosition.Left > bounds.Right ||
                wp.NormalPosition.Right < bounds.Left ||
                wp.NormalPosition.Top > bounds.Bottom ||
                wp.NormalPosition.Bottom < bounds.Top)
            {
                // Center on primary monitor
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

    private static void RestoreDocker(RestoreResult result)
    {
        if (DockerHelper.IsDockerRunning())
        {
            result.Warnings.Add("Docker Desktop is already running.");
            return;
        }

        if (!DockerHelper.LaunchDocker())
        {
            result.Errors.Add("Docker Desktop not found or failed to launch.");
        }
    }
}
