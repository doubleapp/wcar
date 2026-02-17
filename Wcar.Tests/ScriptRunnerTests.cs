using Wcar.Config;
using Wcar.Scripts;

namespace Wcar.Tests;

public class ScriptRunnerTests
{
    [Fact]
    public void BuildStartInfo_PowerShell_CorrectExeAndArgs()
    {
        var psi = ScriptRunner.BuildStartInfo("Write-Host Hello", ScriptShell.PowerShell);

        Assert.Equal("powershell.exe", psi.FileName);
        Assert.Contains("-NoExit", psi.Arguments);
        Assert.Contains("-Command", psi.Arguments);
        Assert.Contains("Write-Host Hello", psi.Arguments);
    }

    [Fact]
    public void BuildStartInfo_Pwsh_CorrectExeAndArgs()
    {
        var psi = ScriptRunner.BuildStartInfo("Write-Host Hello", ScriptShell.Pwsh);

        Assert.Equal("pwsh.exe", psi.FileName);
        Assert.Contains("-NoExit", psi.Arguments);
        Assert.Contains("-Command", psi.Arguments);
    }

    [Fact]
    public void BuildStartInfo_Cmd_UsesSlashK()
    {
        var psi = ScriptRunner.BuildStartInfo("dir /s", ScriptShell.Cmd);

        Assert.Equal("cmd.exe", psi.FileName);
        Assert.Contains("/K", psi.Arguments);
        Assert.Contains("dir /s", psi.Arguments);
    }

    [Fact]
    public void BuildStartInfo_Bash_UsesWslBashC()
    {
        var psi = ScriptRunner.BuildStartInfo("ls -la", ScriptShell.Bash);

        Assert.Equal("wsl.exe", psi.FileName);
        Assert.Contains("bash -c", psi.Arguments);
        Assert.Contains("ls -la", psi.Arguments);
    }
}
