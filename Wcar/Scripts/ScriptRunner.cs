using System.Diagnostics;

namespace Wcar.Scripts;

public static class ScriptRunner
{
    public static bool Run(string command)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoExit -Command \"{EscapeCommand(command)}\"",
                UseShellExecute = true,
                CreateNoWindow = false
            };

            var proc = Process.Start(psi);
            return proc != null;
        }
        catch
        {
            return false;
        }
    }

    private static string EscapeCommand(string command)
    {
        // Escape double quotes for PowerShell
        return command.Replace("\"", "`\"");
    }
}
