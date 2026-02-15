using System.Diagnostics;
using Wcar.Config;
using Wcar.Interop;

namespace Wcar.Session;

public class WindowEnumerator
{
    private readonly Dictionary<string, bool> _trackedApps;

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

    private static readonly HashSet<string> VsCodeProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Code"
    };

    private static readonly HashSet<string> ExplorerProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "explorer"
    };

    private static readonly Dictionary<string, HashSet<string>> AppKeyToProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Chrome"] = ChromeProcessNames,
        ["VSCode"] = VsCodeProcessNames,
        ["CMD"] = CmdProcessNames,
        ["PowerShell"] = PowerShellProcessNames,
        ["Explorer"] = ExplorerProcessNames,
    };

    public WindowEnumerator(Dictionary<string, bool> trackedApps)
    {
        _trackedApps = trackedApps;
    }

    public SessionSnapshot CaptureSession()
    {
        var snapshot = new SessionSnapshot
        {
            CapturedAt = DateTime.UtcNow,
            DockerDesktopRunning = _trackedApps.GetValueOrDefault("DockerDesktop", false)
                                  && DockerHelper.IsDockerRunning()
        };

        var windows = new List<WindowInfo>();

        NativeMethods.EnumWindows((hWnd, _) =>
        {
            var info = TryCaptureWindow(hWnd);
            if (info != null)
                windows.Add(info);
            return true;
        }, IntPtr.Zero);

        snapshot.Windows = windows;
        return snapshot;
    }

    private WindowInfo? TryCaptureWindow(IntPtr hWnd)
    {
        if (!NativeMethods.IsWindowVisible(hWnd))
            return null;

        // Filter out tool windows and windows without owners that aren't app windows
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

        return info;
    }

    private bool IsTrackedProcess(string processName)
    {
        foreach (var kvp in _trackedApps)
        {
            if (!kvp.Value) continue;

            if (AppKeyToProcessNames.TryGetValue(kvp.Key, out var names))
            {
                if (names.Contains(processName))
                    return true;
            }
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
