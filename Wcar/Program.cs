using Wcar.Config;
using Wcar.Scripts;

namespace Wcar;

static class Program
{
    private const string MutexName = "Global\\WCAR_SingleInstance";

    [STAThread]
    static void Main(string[] args)
    {
        using var mutex = new Mutex(true, MutexName, out bool createdNew);

        if (args.Length > 0)
        {
            HandleCli(args);
            return;
        }

        if (!createdNew)
            return;

        ApplicationConfiguration.Initialize();
        var configManager = new ConfigManager();
        Application.Run(new WcarContext(configManager));
    }

    private static void HandleCli(string[] args)
    {
        switch (args[0].ToLowerInvariant())
        {
            case "add-script":
                HandleAddScript(args);
                break;
            case "edit-script":
                HandleEditScript(args);
                break;
            case "remove-script":
                HandleRemoveScript(args);
                break;
            default:
                PrintUsage();
                break;
        }
    }

    private static void HandleAddScript(string[] args)
    {
        string? name = null;
        string? command = null;
        string? shellStr = null;
        string? description = null;

        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "--name" && i + 1 < args.Length) name = args[++i];
            else if (args[i] == "--command" && i + 1 < args.Length) command = args[++i];
            else if (args[i] == "--shell" && i + 1 < args.Length) shellStr = args[++i];
            else if (args[i] == "--description" && i + 1 < args.Length) description = args[++i];
        }

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(command))
        {
            Console.Error.WriteLine("Usage: wcar.exe add-script --name \"Name\" --command \"C\" [--shell PowerShell|Pwsh|Cmd|Bash] [--description \"D\"]");
            return;
        }

        var shell = ScriptShell.PowerShell;
        if (shellStr != null && !Enum.TryParse(shellStr, true, out shell))
        {
            Console.Error.WriteLine($"Unknown shell: {shellStr}. Valid: PowerShell, Pwsh, Cmd, Bash");
            return;
        }

        var manager = new ScriptManager(new ConfigManager());
        if (manager.AddScript(name, command, shell, description ?? ""))
            Console.WriteLine($"Script '{name}' added successfully.");
        else
            Console.Error.WriteLine($"Script '{name}' already exists or invalid input.");
    }

    private static void HandleEditScript(string[] args)
    {
        string? name = null;
        string? command = null;
        string? shellStr = null;
        string? description = null;

        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "--name" && i + 1 < args.Length) name = args[++i];
            else if (args[i] == "--command" && i + 1 < args.Length) command = args[++i];
            else if (args[i] == "--shell" && i + 1 < args.Length) shellStr = args[++i];
            else if (args[i] == "--description" && i + 1 < args.Length) description = args[++i];
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            Console.Error.WriteLine("Usage: wcar.exe edit-script --name \"Name\" [--command \"C\"] [--shell Shell] [--description \"D\"]");
            return;
        }

        ScriptShell? shell = null;
        if (shellStr != null)
        {
            if (!Enum.TryParse<ScriptShell>(shellStr, true, out var parsed))
            {
                Console.Error.WriteLine($"Unknown shell: {shellStr}. Valid: PowerShell, Pwsh, Cmd, Bash");
                return;
            }
            shell = parsed;
        }

        var manager = new ScriptManager(new ConfigManager());
        if (manager.EditScript(name, command, shell, description))
            Console.WriteLine($"Script '{name}' updated successfully.");
        else
            Console.Error.WriteLine($"Script '{name}' not found.");
    }

    private static void HandleRemoveScript(string[] args)
    {
        string? name = null;

        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "--name" && i + 1 < args.Length) name = args[++i];
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            Console.Error.WriteLine("Usage: wcar.exe remove-script --name \"Name\"");
            return;
        }

        var manager = new ScriptManager(new ConfigManager());
        if (manager.RemoveScript(name))
            Console.WriteLine($"Script '{name}' removed successfully.");
        else
            Console.Error.WriteLine($"Script '{name}' not found.");
    }

    private static void PrintUsage()
    {
        Console.Error.WriteLine("WCAR - Window Configuration Auto Restorer");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Usage:");
        Console.Error.WriteLine("  wcar.exe                                          Start tray app");
        Console.Error.WriteLine("  wcar.exe add-script --name \"N\" --command \"C\"      Add a script");
        Console.Error.WriteLine("    [--shell PowerShell|Pwsh|Cmd|Bash] [--description \"D\"]");
        Console.Error.WriteLine("  wcar.exe edit-script --name \"N\"                   Edit a script");
        Console.Error.WriteLine("    [--command \"C\"] [--shell Shell] [--description \"D\"]");
        Console.Error.WriteLine("  wcar.exe remove-script --name \"N\"                 Remove a script");
    }
}
