using Wcar.Session;

namespace Wcar.Tests;

public class WindowMatcherTests
{
    private static WindowInfo MakeWindow(string title, int zOrder = 0) =>
        new() { Title = title, ProcessName = "test", ZOrder = zOrder };

    private static (IntPtr Handle, string Title) MakeActual(string title, int handleId) =>
        (new IntPtr(handleId), title);

    [Fact]
    public void Match_TitleMatch_ReturnsCorrectPairs()
    {
        var saved = new[]
        {
            MakeWindow("wcar - Visual Studio Code"),
            MakeWindow("Google Chrome")
        };

        var actual = new[]
        {
            MakeActual("wcar - Visual Studio Code", 1001),
            MakeActual("Google Chrome", 1002)
        };

        var matches = WindowMatcher.Match(saved, actual);

        Assert.Equal(2, matches.Count);
        Assert.Contains(matches, m => m.SavedIndex == 0 && m.ActualHandle == new IntPtr(1001));
        Assert.Contains(matches, m => m.SavedIndex == 1 && m.ActualHandle == new IntPtr(1002));
    }

    [Fact]
    public void Match_NoTitleMatch_FallsBackToIndexOrder()
    {
        var saved = new[]
        {
            MakeWindow("Old Title 1"),
            MakeWindow("Old Title 2")
        };

        var actual = new[]
        {
            MakeActual("New Title A", 2001),
            MakeActual("New Title B", 2002)
        };

        var matches = WindowMatcher.Match(saved, actual);

        // Index fallback: saved[0] → actual[0], saved[1] → actual[1]
        Assert.Equal(2, matches.Count);
        Assert.Contains(matches, m => m.SavedIndex == 0 && m.ActualHandle == new IntPtr(2001));
        Assert.Contains(matches, m => m.SavedIndex == 1 && m.ActualHandle == new IntPtr(2002));
    }

    [Fact]
    public void Match_MoreActualThanSaved_ExtraActualIgnored()
    {
        var saved = new[] { MakeWindow("App Window") };
        var actual = new[]
        {
            MakeActual("App Window", 3001),
            MakeActual("Other Window", 3002) // extra — should be ignored
        };

        var matches = WindowMatcher.Match(saved, actual);

        Assert.Single(matches);
        Assert.Equal(0, matches[0].SavedIndex);
        Assert.Equal(new IntPtr(3001), matches[0].ActualHandle);
    }

    [Fact]
    public void Match_MoreSavedThanActual_ExtraSavedSkipped()
    {
        var saved = new[]
        {
            MakeWindow("Window 1"),
            MakeWindow("Window 2"), // No matching actual
        };
        var actual = new[] { MakeActual("Window 1", 4001) };

        var matches = WindowMatcher.Match(saved, actual);

        // Only 1 match because there's only 1 actual window
        Assert.Single(matches);
        Assert.Equal(new IntPtr(4001), matches[0].ActualHandle);
    }
}
