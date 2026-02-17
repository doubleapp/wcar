using System.Diagnostics;
using System.Runtime.InteropServices;
using Wcar.Config;

namespace Wcar.Session;

public interface IShortcutScanner
{
    IEnumerable<DiscoveredApp> Scan();
}

public interface IProcessScanner
{
    IEnumerable<DiscoveredApp> Scan();
}

public class StartMenuScanner : IShortcutScanner
{
    private static readonly string[] StartMenuPaths =
    {
        Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
        Environment.GetFolderPath(Environment.SpecialFolder.StartMenu)
    };

    public IEnumerable<DiscoveredApp> Scan()
    {
        var results = new List<DiscoveredApp>();

        foreach (var root in StartMenuPaths)
        {
            if (!Directory.Exists(root)) continue;

            foreach (var lnk in Directory.EnumerateFiles(root, "*.lnk", SearchOption.AllDirectories))
            {
                try
                {
                    var target = ResolveShortcut(lnk);
                    if (target == null) continue;
                    if (!target.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) continue;

                    var displayName = Path.GetFileNameWithoutExtension(lnk);
                    var processName = Path.GetFileNameWithoutExtension(target);

                    results.Add(new DiscoveredApp
                    {
                        DisplayName = displayName,
                        ProcessName = processName,
                        ExecutablePath = target,
                        Source = AppSource.StartMenu
                    });
                }
                catch
                {
                    // Skip broken shortcuts
                }
            }
        }

        return results;
    }

    private static string? ResolveShortcut(string lnkPath)
    {
        try
        {
            var shellLink = (IShellLinkW)new ShellLink();
            var persistFile = (IPersistFile)shellLink;
            persistFile.Load(lnkPath, 0);

            var targetBuffer = new char[260];
            shellLink.GetPath(targetBuffer, targetBuffer.Length, IntPtr.Zero, 0);
            var target = new string(targetBuffer).TrimEnd('\0');
            return string.IsNullOrEmpty(target) ? null : target;
        }
        catch
        {
            return null;
        }
    }

    [ComImport, Guid("00021401-0000-0000-C000-000000000046")]
    private class ShellLink { }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
     Guid("000214F9-0000-0000-C000-000000000046")]
    private interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] char[] pszFile, int cch, IntPtr pfd, int fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] char[] pszName, int cch);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] char[] pszDir, int cch);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] char[] pszArgs, int cch);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] char[] pszIconPath, int cch, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
        void Resolve(IntPtr hwnd, int fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
     Guid("0000010b-0000-0000-C000-000000000046")]
    private interface IPersistFile
    {
        void GetClassID(out Guid pClassID);
        void IsDirty();
        void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, int dwMode);
        void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, bool fRemember);
        void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
        void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
    }
}

public class RunningProcessScanner : IProcessScanner
{
    public IEnumerable<DiscoveredApp> Scan()
    {
        var results = new List<DiscoveredApp>();

        foreach (var proc in Process.GetProcesses())
        {
            try
            {
                if (string.IsNullOrEmpty(proc.MainWindowTitle)) continue;

                string? exePath = null;
                try { exePath = proc.MainModule?.FileName; } catch { }

                results.Add(new DiscoveredApp
                {
                    DisplayName = proc.MainWindowTitle,
                    ProcessName = proc.ProcessName,
                    ExecutablePath = exePath,
                    Source = AppSource.RunningProcess
                });
            }
            catch
            {
                // Skip inaccessible processes
            }
            finally
            {
                proc.Dispose();
            }
        }

        return results;
    }
}

public static class AppDiscoveryService
{
    /// <summary>
    /// Pure function: merges installed + running results, deduplicates by executable path,
    /// and filters by search query. Start Menu entries are preferred for display names.
    /// </summary>
    public static List<DiscoveredApp> FilterAndMerge(
        IEnumerable<DiscoveredApp> installed,
        IEnumerable<DiscoveredApp> running,
        string query = "")
    {
        var merged = new Dictionary<string, DiscoveredApp>(StringComparer.OrdinalIgnoreCase);

        // Add installed apps first (preferred display names)
        foreach (var app in installed)
        {
            var key = app.ExecutablePath ?? app.ProcessName;
            merged[key] = app;
        }

        // Add running processes — only if not already present by executable path
        foreach (var app in running)
        {
            var key = app.ExecutablePath ?? app.ProcessName;
            if (!merged.ContainsKey(key))
                merged[key] = app;
        }

        var all = merged.Values.ToList();

        if (string.IsNullOrWhiteSpace(query))
            return all.OrderBy(a => a.DisplayName).ToList();

        return all
            .Where(a => MatchesQuery(a, query))
            .OrderBy(a => a.DisplayName)
            .ToList();
    }

    private static bool MatchesQuery(DiscoveredApp app, string query)
    {
        return app.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase)
            || app.ProcessName.Contains(query, StringComparison.OrdinalIgnoreCase)
            || (app.ExecutablePath?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false);
    }
}
