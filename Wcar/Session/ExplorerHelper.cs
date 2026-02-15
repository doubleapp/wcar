using System.Runtime.InteropServices;

namespace Wcar.Session;

public static class ExplorerHelper
{
    public static string? GetFolderPathForExplorerWindow(IntPtr hwnd)
    {
        try
        {
            var shellType = Type.GetTypeFromProgID("Shell.Application");
            if (shellType == null) return null;

            var shell = Activator.CreateInstance(shellType);
            if (shell == null) return null;

            try
            {
                var windows = shell.GetType().InvokeMember("Windows",
                    System.Reflection.BindingFlags.InvokeMethod, null, shell, null);
                if (windows == null) return null;

                var count = (int)windows.GetType().InvokeMember("Count",
                    System.Reflection.BindingFlags.GetProperty, null, windows, null)!;

                for (int i = 0; i < count; i++)
                {
                    var window = windows.GetType().InvokeMember("Item",
                        System.Reflection.BindingFlags.InvokeMethod, null, windows, new object[] { i });
                    if (window == null) continue;

                    try
                    {
                        var windowHwnd = (long)window.GetType().InvokeMember("HWND",
                            System.Reflection.BindingFlags.GetProperty, null, window, null)!;

                        if (new IntPtr(windowHwnd) == hwnd)
                        {
                            var doc = window.GetType().InvokeMember("Document",
                                System.Reflection.BindingFlags.GetProperty, null, window, null);
                            if (doc == null) continue;

                            var folder = doc.GetType().InvokeMember("Folder",
                                System.Reflection.BindingFlags.GetProperty, null, doc, null);
                            if (folder == null) continue;

                            var self = folder.GetType().InvokeMember("Self",
                                System.Reflection.BindingFlags.GetProperty, null, folder, null);
                            if (self == null) continue;

                            var path = (string?)self.GetType().InvokeMember("Path",
                                System.Reflection.BindingFlags.GetProperty, null, self, null);

                            return path;
                        }
                    }
                    finally
                    {
                        if (window != null)
                            Marshal.ReleaseComObject(window);
                    }
                }

                Marshal.ReleaseComObject(windows);
                return null;
            }
            finally
            {
                Marshal.ReleaseComObject(shell);
            }
        }
        catch
        {
            return null;
        }
    }
}
