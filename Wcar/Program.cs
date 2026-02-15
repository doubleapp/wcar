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
        {
            MessageBox.Show("WCAR is already running.", "WCAR",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

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
        if (!RequireElevation()) return;

        string? name = null;
        string? command = null;

        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "--name" && i + 1 < args.Length) name = args[++i];
            else if (args[i] == "--command" && i + 1 < args.Length) command = args[++i];
        }

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(command))
        {
            Console.Error.WriteLine("Usage: wcar.exe add-script --name \"Name\" --command \"Command\"");
            return;
        }

        var manager = new ScriptManager(new ConfigManager());
        if (manager.AddScript(name, command))
            Console.WriteLine($"Script '{name}' added successfully.");
        else
            Console.Error.WriteLine($"Script '{name}' already exists or invalid input.");
    }

    private static void HandleRemoveScript(string[] args)
    {
        if (!RequireElevation()) return;

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

    private static bool RequireElevation()
    {
        if (UacHelper.IsElevated())
            return true;

        Console.Error.WriteLine("Error: Script management requires administrator privileges.");
        Console.Error.WriteLine("Please run this command from an elevated (admin) prompt.");
        return false;
    }

    private static void PrintUsage()
    {
        Console.Error.WriteLine("WCAR - Window Configuration Auto Restorer");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Usage:");
        Console.Error.WriteLine("  wcar.exe                                          Start tray app");
        Console.Error.WriteLine("  wcar.exe add-script --name \"N\" --command \"C\"      Add a script (requires admin)");
        Console.Error.WriteLine("  wcar.exe remove-script --name \"N\"                 Remove a script (requires admin)");
    }
}
