using System.Drawing.Imaging;

namespace Wcar.Session;

public interface IScreenCapture
{
    void CaptureAll(string outputDirectory);
    void Cleanup(string outputDirectory, int monitorCount);
}

public class ScreenCaptureService : IScreenCapture
{
    public void CaptureAll(string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);

        var screens = Screen.AllScreens;
        for (int i = 0; i < screens.Length; i++)
        {
            var screen = screens[i];
            using var bitmap = new Bitmap(screen.Bounds.Width, screen.Bounds.Height);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(screen.Bounds.Location, Point.Empty, screen.Bounds.Size);
            var path = ScreenshotHelper.GetScreenshotPath(outputDirectory, i);
            bitmap.Save(path, ImageFormat.Png);
        }
    }

    public void Cleanup(string outputDirectory, int monitorCount)
    {
        if (!Directory.Exists(outputDirectory)) return;

        // Delete screenshot files for monitors that no longer exist
        int i = monitorCount;
        while (true)
        {
            var path = ScreenshotHelper.GetScreenshotPath(outputDirectory, i);
            if (!File.Exists(path)) break;
            File.Delete(path);
            i++;
        }
    }
}

public static class ScreenshotHelper
{
    public static string GetScreenshotDirectory(string dataDir) =>
        Path.Combine(dataDir, "screenshots");

    public static string GetScreenshotPath(string outputDirectory, int monitorIndex) =>
        Path.Combine(outputDirectory, $"monitor_{monitorIndex}.png");

    public static bool HasScreenshots(string dataDir)
    {
        var dir = GetScreenshotDirectory(dataDir);
        if (!Directory.Exists(dir)) return false;
        return File.Exists(GetScreenshotPath(dir, 0));
    }

    /// <summary>
    /// Captures screenshots on a background thread (fire-and-forget).
    /// Session data should be saved before calling this.
    /// </summary>
    public static void CaptureAsync(string dataDir, IScreenCapture? capture = null)
    {
        var service = capture ?? new ScreenCaptureService();
        var dir = GetScreenshotDirectory(dataDir);
        var monitorCount = Screen.AllScreens.Length;

        Task.Run(() =>
        {
            try
            {
                service.CaptureAll(dir);
                service.Cleanup(dir, monitorCount);
            }
            catch (Exception ex)
            {
                // Screenshot failure is non-critical; log as warning
                System.Diagnostics.Debug.WriteLine($"[WCAR] Screenshot capture failed: {ex.Message}");
            }
        });
    }
}

