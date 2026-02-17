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
        var trackedApps = AppConfig.DefaultTrackedApps()
            .Select(a => new TrackedApp
            {
                DisplayName = a.DisplayName,
                ProcessName = a.ProcessName,
                ExecutablePath = a.ExecutablePath,
                Launch = a.Launch,
                Enabled = false   // all disabled
            }).ToList();

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

    [Fact]
    public void CaptureSession_ZOrderIsAssignedSequentially()
    {
        var config = new AppConfig();
        var enumerator = new WindowEnumerator(config.TrackedApps);

        var snapshot = enumerator.CaptureSession();

        if (snapshot.Windows.Count >= 2)
        {
            // ZOrder should be sequential 0, 1, 2, ...
            for (int i = 0; i < snapshot.Windows.Count; i++)
                Assert.Equal(i, snapshot.Windows[i].ZOrder);
        }
    }

    [Fact]
    public void CaptureSession_IncludesMonitorList()
    {
        var config = new AppConfig();
        var enumerator = new WindowEnumerator(config.TrackedApps);

        var snapshot = enumerator.CaptureSession();

        // Should capture monitors (at least 1)
        Assert.NotNull(snapshot.Monitors);
        Assert.True(snapshot.Monitors.Count >= 1);
    }

    [Fact]
    public void CaptureSession_DynamicApp_ByProcessName()
    {
        // Add a custom tracked app and verify the enumerator respects it
        var tracked = new List<TrackedApp>
        {
            new() { DisplayName = "Notepad", ProcessName = "notepad", Enabled = true, Launch = LaunchStrategy.LaunchPerWindow }
        };
        var enumerator = new WindowEnumerator(tracked);

        // Should not throw — just capture whatever notepad windows exist
        var exception = Record.Exception(() => enumerator.CaptureSession());
        Assert.Null(exception);
    }
}
