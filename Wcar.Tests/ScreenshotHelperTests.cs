using Wcar.Session;

namespace Wcar.Tests;

public class ScreenshotHelperTests : IDisposable
{
    private readonly string _testDir;

    public ScreenshotHelperTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "wcar_screenshot_test_" + Guid.NewGuid().ToString("N"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, recursive: true);
    }

    [Fact]
    public void GetScreenshotDirectory_ReturnsCorrectPath()
    {
        var dir = ScreenshotHelper.GetScreenshotDirectory(_testDir);
        Assert.Equal(Path.Combine(_testDir, "screenshots"), dir);
    }

    [Fact]
    public void GetScreenshotPath_ReturnsCorrectFilename()
    {
        var dir = @"C:\screenshots";
        var path = ScreenshotHelper.GetScreenshotPath(dir, 0);
        Assert.Equal(Path.Combine(dir, "monitor_0.png"), path);

        var path2 = ScreenshotHelper.GetScreenshotPath(dir, 2);
        Assert.Equal(Path.Combine(dir, "monitor_2.png"), path2);
    }

    [Fact]
    public void HasScreenshots_NoDirectory_ReturnsFalse()
    {
        Assert.False(ScreenshotHelper.HasScreenshots(_testDir));
    }

    [Fact]
    public void HasScreenshots_WithMonitorZeroPng_ReturnsTrue()
    {
        var screenshotDir = ScreenshotHelper.GetScreenshotDirectory(_testDir);
        Directory.CreateDirectory(screenshotDir);
        File.WriteAllBytes(ScreenshotHelper.GetScreenshotPath(screenshotDir, 0), new byte[] { 0x89, 0x50 });

        Assert.True(ScreenshotHelper.HasScreenshots(_testDir));
    }
}
