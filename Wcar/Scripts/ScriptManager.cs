using Wcar.Config;

namespace Wcar.Scripts;

public class ScriptManager
{
    private readonly ConfigManager _configManager;

    public ScriptManager(ConfigManager configManager)
    {
        _configManager = configManager;
    }

    public bool AddScript(string name, string command,
        ScriptShell shell = ScriptShell.PowerShell, string description = "")
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(command))
            return false;

        var config = _configManager.Load();

        if (config.Scripts.Any(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            return false;

        config.Scripts.Add(new ScriptEntry(name, command, shell, description));
        _configManager.Save(config);
        return true;
    }

    public bool RemoveScript(string name)
    {
        var config = _configManager.Load();
        var removed = config.Scripts.RemoveAll(
            s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (removed == 0)
            return false;

        _configManager.Save(config);
        return true;
    }

    public bool EditScript(string name, string? newCommand = null,
        ScriptShell? shell = null, string? description = null)
    {
        var config = _configManager.Load();
        var script = config.Scripts.FirstOrDefault(
            s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (script == null)
            return false;

        if (newCommand != null) script.Command = newCommand;
        if (shell != null) script.Shell = shell.Value;
        if (description != null) script.Description = description;

        _configManager.Save(config);
        return true;
    }

    public List<ScriptEntry> GetScripts()
    {
        return _configManager.Load().Scripts;
    }
}
