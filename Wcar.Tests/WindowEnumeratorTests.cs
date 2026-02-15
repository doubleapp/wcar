using Wcar.Config;
using Wcar.Session;

namespace Wcar.Tests;

public class WindowEnumeratorTests
{
    [Fact]
    public void CaptureSession_WithDefaultTrackedApps_DoesNotThrow()
    {
        var config = new AppConfig();
        var enumerator = new WindowEnumerator(config.TrackedApps);

        var exception = Record.Exception(() => enumerator.CaptureSession());

        Assert.Null(exception);
    }

    [Fact]
    public void CaptureSession_ReturnsSnapshot_WithTimestamp()
    {
        var config = new AppConfig();
        var enumerator = new WindowEnumerator(config.TrackedApps);

        var snapshot = enumerator.CaptureSession();

        Assert.NotNull(snapshot);
        Assert.True(snapshot.CapturedAt <= DateTime.UtcNow);
        Assert.True(snapshot.CapturedAt > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public void CaptureSession_WithAllAppsDisabled_ReturnsEmptyWindows()
    {
        var trackedApps = new Dictionary<string, bool>
        {
            ["Chrome"] = false,
            ["VSCode"] = false,
            ["CMD"] = false,
            ["PowerShell"] = false,
            ["Explorer"] = false,
            ["DockerDesktop"] = false
        };
        var enumerator = new WindowEnumerator(trackedApps);

        var snapshot = enumerator.CaptureSession();

        Assert.Empty(snapshot.Windows);
    }

    [Fact]
    public void CaptureSession_WindowInfoHasRequiredFields()
    {
        var config = new AppConfig();
        var enumerator = new WindowEnumerator(config.TrackedApps);

        var snapshot = enumerator.CaptureSession();

        foreach (var win in snapshot.Windows)
        {
            Assert.False(string.IsNullOrEmpty(win.ProcessName));
            Assert.True(win.ShowCmd > 0);
        }
    }
}
