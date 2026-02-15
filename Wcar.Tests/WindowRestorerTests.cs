using Wcar.Config;
using Wcar.Session;

namespace Wcar.Tests;

public class WindowRestorerTests
{
    [Fact]
    public void Restore_EmptySnapshot_ReturnsNoErrors()
    {
        var config = new AppConfig();
        var restorer = new WindowRestorer(config.TrackedApps);
        var snapshot = new SessionSnapshot();

        var result = restorer.Restore(snapshot);

        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Restore_UnknownProcess_ReportsError()
    {
        var config = new AppConfig();
        var restorer = new WindowRestorer(config.TrackedApps);
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
    public void Restore_DockerFlagFalse_DoesNotAttemptDockerLaunch()
    {
        var config = new AppConfig();
        var restorer = new WindowRestorer(config.TrackedApps);
        var snapshot = new SessionSnapshot { DockerDesktopRunning = false };

        var result = restorer.Restore(snapshot);

        // No Docker-related errors or warnings expected
        Assert.DoesNotContain(result.Errors, e => e.Contains("Docker"));
        Assert.DoesNotContain(result.Warnings, w => w.Contains("Docker"));
    }

    [Fact]
    public void Restore_NullWorkingDirectory_DefaultsGracefully()
    {
        var config = new AppConfig();
        var restorer = new WindowRestorer(config.TrackedApps);
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

        // Should not throw — null CWD defaults to C:\
        var exception = Record.Exception(() => restorer.Restore(snapshot));
        Assert.Null(exception);
    }
}
