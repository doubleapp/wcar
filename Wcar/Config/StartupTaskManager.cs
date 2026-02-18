using System.Diagnostics;
using Microsoft.Win32;

namespace Wcar.Config;

public class StartupTaskManager
{
    private const string RegistryRunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AutoStartTaskName = "WCAR_AutoStart";

    public bool Register(string taskName, string command)
    {
        // Try schtasks first
        if (TryScheduledTask(taskName, command))
            return true;

        // Fallback to Registry Run key
        return TryRegistryRun(taskName, command);
    }

    public bool Unregister(string taskName)
    {
        var removed = false;

        // Remove from Task Scheduler
        removed |= TryRemoveScheduledTask(taskName);

        // Remove from Registry
        removed |= TryRemoveRegistryRun(taskName);

        return removed;
    }

    public bool IsRegistered(string taskName)
    {
        return IsScheduledTaskRegistered(taskName) || IsRegistryRunRegistered(taskName);
    }

    public bool RegisterAutoStart(string exePath)
    {
        var installPath = GetInstalledExePath(exePath);
        return Register(AutoStartTaskName, $"\"{installPath}\"");
    }

    public bool UnregisterAutoStart()
    {
        return Unregister(AutoStartTaskName);
    }

    public bool IsAutoStartRegistered()
    {
        return IsRegistered(AutoStartTaskName);
    }

    /// <summary>
    /// Copies the exe (and companion ico) to %LOCALAPPDATA%\WCAR so that
    /// the startup path is always on the system drive, avoiding issues with
    /// secondary drives (e.g. exFAT) not being mounted at logon time.
    /// Returns the installed path, or the original path if copy fails.
    /// </summary>
    private static string GetInstalledExePath(string sourceExePath)
    {
        var installDir = ConfigManager.GetDefaultDataDir();
        var installExe = Path.Combine(installDir, Path.GetFileName(sourceExePath));

        // Already running from the install location — nothing to copy
        if (string.Equals(Path.GetFullPath(sourceExePath),
                          Path.GetFullPath(installExe),
                          StringComparison.OrdinalIgnoreCase))
            return installExe;

        try
        {
            Directory.CreateDirectory(installDir);

            // Copy exe (overwrite previous version)
            File.Copy(sourceExePath, installExe, overwrite: true);

            // Copy companion ico if present
            var sourceDir = Path.GetDirectoryName(sourceExePath)!;
            var icoSource = Path.Combine(sourceDir, "wcar.ico");
            if (File.Exists(icoSource))
                File.Copy(icoSource, Path.Combine(installDir, "wcar.ico"), overwrite: true);

            return installExe;
        }
        catch
        {
            // If copy fails, fall back to the original path
            return sourceExePath;
        }
    }

    private static bool TryScheduledTask(string taskName, string command)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "schtasks",
                Arguments = $"/Create /SC ONLOGON /TN \"{taskName}\" /TR \"{command}\" /F /RL LIMITED",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            proc?.WaitForExit(10000);
            return proc?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryRemoveScheduledTask(string taskName)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "schtasks",
                Arguments = $"/Delete /TN \"{taskName}\" /F",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            proc?.WaitForExit(10000);
            return proc?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsScheduledTaskRegistered(string taskName)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "schtasks",
                Arguments = $"/Query /TN \"{taskName}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            proc?.WaitForExit(10000);
            return proc?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryRegistryRun(string taskName, string command)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryRunKey, writable: true);
            key?.SetValue(taskName, command);
            return key != null;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryRemoveRegistryRun(string taskName)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryRunKey, writable: true);
            if (key?.GetValue(taskName) != null)
            {
                key.DeleteValue(taskName);
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsRegistryRunRegistered(string taskName)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryRunKey);
            return key?.GetValue(taskName) != null;
        }
        catch
        {
            return false;
        }
    }
}
