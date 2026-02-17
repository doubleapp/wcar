using System.Text.Json.Serialization;

namespace Wcar.Config;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ScriptShell
{
    PowerShell,
    Pwsh,
    Cmd,
    Bash
}

public class ScriptEntry
{
    public string Name { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public ScriptShell Shell { get; set; } = ScriptShell.PowerShell;
    public string Description { get; set; } = string.Empty;

    public ScriptEntry() { }

    public ScriptEntry(string name, string command,
        ScriptShell shell = ScriptShell.PowerShell, string description = "")
    {
        Name = name;
        Command = command;
        Shell = shell;
        Description = description;
    }
}
