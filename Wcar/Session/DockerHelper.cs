using System.Diagnostics;

namespace Wcar.Session;

/// <summary>Docker Desktop is now a regular tracked app. This class is obsolete.</summary>
[Obsolete("Docker Desktop is now a regular TrackedApp. DockerHelper will be removed in v4.")]
public static class DockerHelper
{
    private static readonly string[] DockerProcessNames =
    {
        "Docker Desktop", "DockerDesktop", "Docker"
    };

    public static bool IsDockerRunning()
    {
        foreach (var name in DockerProcessNames)
        {
            try
            {
                var processes = Process.GetProcessesByName(name);
                if (processes.Length > 0)
                {
                    foreach (var p in processes) p.Dispose();
                    return true;
                }
                foreach (var p in processes) p.Dispose();
            }
            catch
            {
                // Process enumeration can fail for elevated processes
            }
        }
        return false;
    }

    public static string? GetDockerExePath()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "Docker", "Docker", "Docker Desktop.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "Docker", "Docker", "Docker Desktop.exe"),
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    public static bool LaunchDocker()
    {
        var exe = GetDockerExePath();
        if (exe == null)
            return false;

        try
        {
            Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true });
            return true;
        }
        catch
        {
            return false;
        }
    }
}
