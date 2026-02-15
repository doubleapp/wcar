using Wcar.Config;

namespace Wcar.Tests;

public class StartupTaskManagerTests
{
    private readonly StartupTaskManager _manager = new();

    [Fact]
    public void IsAutoStartRegistered_DoesNotThrow()
    {
        var exception = Record.Exception(() => _manager.IsAutoStartRegistered());
        Assert.Null(exception);
    }

    [Fact]
    public void IsDiskCheckRegistered_DoesNotThrow()
    {
        var exception = Record.Exception(() => _manager.IsDiskCheckRegistered());
        Assert.Null(exception);
    }

    [Fact]
    public void IsRegistered_UnknownTask_ReturnsFalse()
    {
        var result = _manager.IsRegistered("WCAR_NonExistent_Test_" + Guid.NewGuid().ToString("N"));
        Assert.False(result);
    }

    [Fact]
    public void RegisterDiskCheck_NoScript_ReturnsFalse()
    {
        // check-disk-space.ps1 doesn't exist next to test DLL
        var result = _manager.RegisterDiskCheck();
        Assert.False(result);
    }
}
