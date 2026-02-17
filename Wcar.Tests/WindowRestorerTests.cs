using System.Diagnostics;
using Wcar.Config;
using Wcar.Session;

namespace Wcar.Tests;

/// <summary>Fake launcher that captures what was started but never actually launches.</summary>
public class FakeProcessLauncher : IProcessLauncher
{
    public List<ProcessStartInfo> Started { get; } = new();
    public bool ShouldFail { get; set; }

    public Process? Start(ProcessStartInfo psi)
    {
        if (ShouldFail) throw new InvalidOperationException("Launch failed");
        Started.Add(psi);
        return null; // Return null — restorer handles null gracefully
    }
}

public class WindowRestorerTests
{
    private static List<TrackedApp> DefaultApps() => AppConfig.DefaultTrackedApps();

    [Fact]
    public void Restore_EmptySnapshot_ReturnsNoErrors()
    {
        var restorer = new WindowRestorer(DefaultApps(), new FakeProcessLauncher());
        var snapshot = new SessionSnapshot();

        var result = restorer.Restore(snapshot);

        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Restore_UnknownProcess_ReportsError()
    {
        var restorer = new WindowRestorer(DefaultApps(), new FakeProcessLauncher());
        var snapshot = new SessionSnapshot
        {
            Windows = new List<WindowInfo>
            {
                new() { ProcessName = "unknown_app_xyz", Title = "Test" }
            }
        };

        var result = restorer.Restore(snapshot);

        Assert.Single(result.Errors);
        Assert.Contains("unknown_app_xyz", result.Errors[0]);
    }

    [Fact]
    public void Restore_NullWorkingDirectory_DefaultsGracefully()
    {
        var launcher = new FakeProcessLauncher();
        var restorer = new WindowRestorer(DefaultApps(), launcher);
        var snapshot = new SessionSnapshot
        {
            Windows = new List<WindowInfo>
            {
                new()
                {
                    ProcessName = "cmd",
                    Title = "Command Prompt",
                    WorkingDirectory = null,
                    ShowCmd = 1
                }
            }
        };

        var exception = Record.Exception(() => restorer.Restore(snapshot));
        Assert.Null(exception);
    }

    [Fact]
    public void BuildProcessStartInfo_Cmd_IncludesCwdArgument()
    {
        var restorer = new WindowRestorer(DefaultApps(), new FakeProcessLauncher());
        var cmdApp = new TrackedApp { DisplayName = "CMD", ProcessName = "cmd", ExecutablePath = "cmd.exe", Launch = LaunchStrategy.LaunchPerWindow };
        var win = new WindowInfo { ProcessName = "cmd", WorkingDirectory = @"C:\Users\Test" };

        var psi = restorer.BuildProcessStartInfo(cmdApp, win);

        Assert.NotNull(psi);
        Assert.Contains(@"C:\Users\Test", psi!.Arguments);
    }

    [Fact]
    public void BuildProcessStartInfo_LaunchOnceApp_UsesExecutablePath()
    {
        var restorer = new WindowRestorer(DefaultApps(), new FakeProcessLauncher());
        var app = new TrackedApp
        {
            DisplayName = "Spotify",
            ProcessName = "Spotify",
            ExecutablePath = @"C:\Spotify\Spotify.exe",
            Launch = LaunchStrategy.LaunchOnce
        };
        var win = new WindowInfo { ProcessName = "Spotify" };

        var psi = restorer.BuildProcessStartInfo(app, win);

        Assert.NotNull(psi);
        Assert.Equal(@"C:\Spotify\Spotify.exe", psi!.FileName);
    }

    [Fact]
    public void Restore_LaunchPerWindow_CmdStartsWithCwd()
    {
        var launcher = new FakeProcessLauncher();
        var restorer = new WindowRestorer(DefaultApps(), launcher);
        var snapshot = new SessionSnapshot
        {
            Windows = new List<WindowInfo>
            {
                new() { ProcessName = "cmd", WorkingDirectory = @"C:\Projects", ShowCmd = 1 }
            }
        };

        restorer.Restore(snapshot);

        Assert.Single(launcher.Started);
        Assert.Contains(@"C:\Projects", launcher.Started[0].Arguments);
    }

    [Fact]
    public void Restore_LaunchPerWindow_PwshStartsWithCwd()
    {
        var launcher = new FakeProcessLauncher();
        var restorer = new WindowRestorer(DefaultApps(), launcher);
        var snapshot = new SessionSnapshot
        {
            Windows = new List<WindowInfo>
            {
                new() { ProcessName = "pwsh", WorkingDirectory = @"C:\Code", ShowCmd = 1 }
            }
        };

        restorer.Restore(snapshot);

        Assert.Single(launcher.Started);
        Assert.Contains(@"C:\Code", launcher.Started[0].Arguments);
    }

    [Fact]
    public void Restore_LaunchOnce_StartsProcessOncePerApp()
    {
        var launcher = new FakeProcessLauncher();
        var apps = new List<TrackedApp>
        {
            new() { DisplayName = "Chrome", ProcessName = "chrome", Launch = LaunchStrategy.LaunchOnce, Enabled = true }
        };
        var restorer = new WindowRestorer(apps, launcher);
        var snapshot = new SessionSnapshot
        {
            Windows = new List<WindowInfo>
            {
                new() { ProcessName = "chrome", Title = "Tab 1" },
                new() { ProcessName = "chrome", Title = "Tab 2" }
            }
        };

        restorer.Restore(snapshot);

        // Should launch chrome exactly once despite two saved windows
        Assert.Single(launcher.Started);
    }

    [Fact]
    public void Restore_ExplorerWithFolder_PassesFolderArg()
    {
        var launcher = new FakeProcessLauncher();
        var restorer = new WindowRestorer(DefaultApps(), launcher);
        var snapshot = new SessionSnapshot
        {
            Windows = new List<WindowInfo>
            {
                new() { ProcessName = "explorer", FolderPath = @"C:\Documents", ShowCmd = 1 }
            }
        };

        restorer.Restore(snapshot);

        Assert.Single(launcher.Started);
        Assert.Contains(@"C:\Documents", launcher.Started[0].Arguments);
    }
}
