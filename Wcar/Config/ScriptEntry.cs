namespace Wcar.Config;

public class ScriptEntry
{
    public string Name { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;

    public ScriptEntry() { }

    public ScriptEntry(string name, string command)
    {
        Name = name;
        Command = command;
    }
}
