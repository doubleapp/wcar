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
    public void IsRegistered_UnknownTask_ReturnsFalse()
    {
        var result = _manager.IsRegistered("WCAR_NonExistent_Test_" + Guid.NewGuid().ToString("N"));
        Assert.False(result);
    }
}
