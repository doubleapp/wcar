using Wcar.Session;

namespace Wcar.Tests;

public class DockerHelperTests
{
    [Fact]
    public void IsDockerRunning_DoesNotThrow()
    {
        // Should not throw regardless of whether Docker is installed
        var exception = Record.Exception(() => DockerHelper.IsDockerRunning());
        Assert.Null(exception);
    }

    [Fact]
    public void GetDockerExePath_ReturnsNullOrValidPath()
    {
        var path = DockerHelper.GetDockerExePath();

        // Either null (Docker not installed) or a valid file path
        if (path != null)
        {
            Assert.True(File.Exists(path), $"Returned path does not exist: {path}");
        }
    }

    [Fact]
    public void LaunchDocker_ReturnsFalseWhenNotInstalled()
    {
        // If Docker is not installed, should return false without throwing
        var exePath = DockerHelper.GetDockerExePath();
        if (exePath == null)
        {
            Assert.False(DockerHelper.LaunchDocker());
        }
        // If Docker IS installed, we skip this assertion to avoid launching it
    }
}
