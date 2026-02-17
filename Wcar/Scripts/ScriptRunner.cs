using System.Diagnostics;
using Wcar.Config;

namespace Wcar.Scripts;

public static class ScriptRunner
{
    public static bool Run(string command, ScriptShell shell = ScriptShell.PowerShell)
    {
        try
        {
            var psi = BuildStartInfo(command, shell);
            var proc = Process.Start(psi);
            return proc != null;
        }
        catch
        {
            return false;
        }
    }

    internal static ProcessStartInfo BuildStartInfo(string command, ScriptShell shell)
    {
        return shell switch
        {
            ScriptShell.PowerShell => new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoExit -Command \"{EscapePowerShell(command)}\"",
                UseShellExecute = true,
                CreateNoWindow = false
            },
            ScriptShell.Pwsh => new ProcessStartInfo
            {
                FileName = "pwsh.exe",
                Arguments = $"-NoExit -Command \"{EscapePowerShell(command)}\"",
                UseShellExecute = true,
                CreateNoWindow = false
            },
            ScriptShell.Cmd => new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/K {command}",
                UseShellExecute = true,
                CreateNoWindow = false
            },
            ScriptShell.Bash => new ProcessStartInfo
            {
                FileName = "wsl.exe",
                Arguments = $"bash -c \"{EscapeBash(command)}\"",
                UseShellExecute = true,
                CreateNoWindow = false
            },
            _ => throw new ArgumentOutOfRangeException(nameof(shell), shell, "Unknown shell type")
        };
    }

    internal static string EscapePowerShell(string command)
    {
        return command.Replace("\"", "`\"");
    }

    internal static string EscapeBash(string command)
    {
        return command.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
