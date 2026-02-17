using System.Diagnostics;
using Wcar.Config;
using Wcar.Interop;

namespace Wcar.Session;

public class WindowEnumerator
{
    private readonly List<TrackedApp> _trackedApps;
    private readonly IMonitorProvider _monitorProvider;

    private static readonly HashSet<string> SelfProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "wcar"
    };

    private static readonly HashSet<string> CmdProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "cmd"
    };

    private static readonly HashSet<string> PowerShellProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "powershell", "pwsh"
    };

    private static readonly HashSet<string> ChromeProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "chrome"
    };

    private static readonly HashSet<string> ExplorerProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "explorer"
    };

    public WindowEnumerator(List<TrackedApp> trackedApps, IMonitorProvider? monitorProvider = null)
    {
        _trackedApps = trackedApps;
        _monitorProvider = monitorProvider ?? new MonitorProvider();
    }

    public SessionSnapshot CaptureSession()
    {
        MonitorInfo[] monitors;
        try
        {
            monitors = _monitorProvider.GetMonitors();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WCAR] MonitorProvider failed: {ex.Message}");
            monitors = Array.Empty<MonitorInfo>();
        }

        var snapshot = new SessionSnapshot
        {
            CapturedAt = DateTime.UtcNow,
            Monitors = monitors.ToList()
        };

        var windows = new List<WindowInfo>();
        int zOrder = 0;

        NativeMethods.EnumWindows((hWnd, _) =>
        {
            var info = TryCaptureWindow(hWnd, monitors);
            if (info != null)
            {
                info.ZOrder = zOrder++;
                windows.Add(info);
            }
            return true;
        }, IntPtr.Zero);

        snapshot.Windows = windows;
        return snapshot;
    }

    private WindowInfo? TryCaptureWindow(IntPtr hWnd, MonitorInfo[] monitors)
    {
        if (!NativeMethods.IsWindowVisible(hWnd))
            return null;

        var exStyle = NativeMethods.GetWindowLong(hWnd, NativeMethods.GWL_EXSTYLE);
        if ((exStyle & NativeConstants.WS_EX_TOOLWINDOW) != 0)
            return null;

        NativeMethods.GetWindowThreadProcessId(hWnd, out uint processId);
        if (processId == 0) return null;

        string processName;
        try
        {
            using var proc = Process.GetProcessById((int)processId);
            processName = proc.ProcessName;
        }
        catch
        {
            return null;
        }

        if (SelfProcessNames.Contains(processName))
            return null;

        if (!IsTrackedProcess(processName))
            return null;

        // Chrome filter: only capture windows with titles (skip background)
        if (ChromeProcessNames.Contains(processName))
        {
            var title = GetWindowTitle(hWnd);
            if (string.IsNullOrWhiteSpace(title))
                return null;
        }

        // Explorer filter: skip the shell window (desktop)
        if (ExplorerProcessNames.Contains(processName))
        {
            var title = GetWindowTitle(hWnd);
            if (string.IsNullOrEmpty(title) || title == "Program Manager")
                return null;
        }

        var wp = WINDOWPLACEMENT.Default;
        NativeMethods.GetWindowPlacement(hWnd, ref wp);

        var info = new WindowInfo
        {
            ProcessName = processName,
            Title = GetWindowTitle(hWnd),
            Left = wp.NormalPosition.Left,
            Top = wp.NormalPosition.Top,
            Width = wp.NormalPosition.Width,
            Height = wp.NormalPosition.Height,
            ShowCmd = wp.ShowCmd
        };

        // Read CWD for CMD/PowerShell
        if (CmdProcessNames.Contains(processName) || PowerShellProcessNames.Contains(processName))
        {
            info.WorkingDirectory = WorkingDirectoryReader.GetWorkingDirectory(processId);
        }

        // Read folder path for Explorer
        if (ExplorerProcessNames.Contains(processName))
        {
            info.FolderPath = ExplorerHelper.GetFolderPathForExplorerWindow(hWnd);
        }

        // Assign monitor index based on window center point
        if (monitors.Length > 0)
        {
            try
            {
                info.MonitorIndex = MonitorHelper.AssignMonitorIndex(
                    info.Left, info.Top, info.Width, info.Height, monitors);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WCAR] AssignMonitorIndex failed: {ex.Message}");
            }
        }

        return info;
    }

    private bool IsTrackedProcess(string processName)
    {
        foreach (var app in _trackedApps)
        {
            if (!app.Enabled) continue;
            if (app.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static string GetWindowTitle(IntPtr hWnd)
    {
        int length = NativeMethods.GetWindowTextLength(hWnd);
        if (length == 0) return string.Empty;

        var buffer = new char[length + 1];
        NativeMethods.GetWindowText(hWnd, buffer, buffer.Length);
        return new string(buffer, 0, length);
    }
}
