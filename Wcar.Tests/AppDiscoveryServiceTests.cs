using Wcar.Config;
using Wcar.Session;

namespace Wcar.Tests;

public class AppDiscoveryServiceTests
{
    private static DiscoveredApp MakeApp(string display, string process, string? path = null,
        AppSource source = AppSource.StartMenu) =>
        new() { DisplayName = display, ProcessName = process, ExecutablePath = path, Source = source };

    [Fact]
    public void FilterAndMerge_EmptyInputs_ReturnsEmpty()
    {
        var result = AppDiscoveryService.FilterAndMerge(
            Array.Empty<DiscoveredApp>(), Array.Empty<DiscoveredApp>());

        Assert.Empty(result);
    }

    [Fact]
    public void FilterAndMerge_DeduplicatesByExecutablePath()
    {
        var installed = new[] { MakeApp("Google Chrome", "chrome", @"C:\chrome.exe") };
        var running = new[] { MakeApp("chrome", "chrome", @"C:\chrome.exe", AppSource.RunningProcess) };

        var result = AppDiscoveryService.FilterAndMerge(installed, running);

        Assert.Single(result);
        Assert.Equal("Google Chrome", result[0].DisplayName); // Start Menu name preferred
    }

    [Fact]
    public void FilterAndMerge_WithQuery_FiltersCorrectly()
    {
        var installed = new[]
        {
            MakeApp("Google Chrome", "chrome"),
            MakeApp("Visual Studio Code", "Code"),
            MakeApp("Notepad", "notepad")
        };

        var result = AppDiscoveryService.FilterAndMerge(installed, Array.Empty<DiscoveredApp>(), "chrome");

        Assert.Single(result);
        Assert.Equal("Google Chrome", result[0].DisplayName);
    }

    [Fact]
    public void FilterAndMerge_QueryMatchesProcessName()
    {
        var installed = new[] { MakeApp("Visual Studio Code", "Code") };

        var result = AppDiscoveryService.FilterAndMerge(installed, Array.Empty<DiscoveredApp>(), "code");

        Assert.Single(result);
    }

    [Fact]
    public void FilterAndMerge_RunningOnlyApps_AddedIfNotInInstalled()
    {
        var installed = new[] { MakeApp("Google Chrome", "chrome", @"C:\chrome.exe") };
        var running = new[]
        {
            MakeApp("chrome", "chrome", @"C:\chrome.exe", AppSource.RunningProcess), // duplicate
            MakeApp("Spotify.exe", "Spotify", @"C:\Spotify.exe", AppSource.RunningProcess) // unique
        };

        var result = AppDiscoveryService.FilterAndMerge(installed, running);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, a => a.ProcessName == "Spotify");
    }
}
