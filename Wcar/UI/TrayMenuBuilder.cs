using Wcar.Config;

namespace Wcar.UI;

public class TrayMenuBuilder
{
    private readonly AppConfig _config;
    private readonly NotifyIcon _notifyIcon;

    public event EventHandler? SaveSessionClicked;
    public event EventHandler? RestoreSessionClicked;
    public event EventHandler? SettingsClicked;
    public event EventHandler? ExitClicked;
    public event EventHandler<ScriptEntry>? ScriptClicked;

    public TrayMenuBuilder(AppConfig config, NotifyIcon notifyIcon)
    {
        _config = config;
        _notifyIcon = notifyIcon;
    }

    public ContextMenuStrip Build()
    {
        var menu = new ContextMenuStrip();

        menu.Items.Add("Save Session", null, OnSaveSession);
        menu.Items.Add("Restore Session", null, OnRestoreSession);
        menu.Items.Add(new ToolStripSeparator());

        var scriptsMenu = BuildScriptsMenu();
        menu.Items.Add(scriptsMenu);

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Settings", null, OnSettings);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, OnExit);

        return menu;
    }

    public void RefreshMenu(AppConfig config)
    {
        _notifyIcon.ContextMenuStrip?.Dispose();
        var builder = new TrayMenuBuilder(config, _notifyIcon)
        {
            SaveSessionClicked = SaveSessionClicked,
            RestoreSessionClicked = RestoreSessionClicked,
            SettingsClicked = SettingsClicked,
            ExitClicked = ExitClicked,
            ScriptClicked = ScriptClicked
        };
        _notifyIcon.ContextMenuStrip = builder.Build();
    }

    private ToolStripMenuItem BuildScriptsMenu()
    {
        var scriptsItem = new ToolStripMenuItem("Scripts");

        if (_config.Scripts.Count == 0)
        {
            var noScripts = new ToolStripMenuItem("(No scripts configured)")
            {
                Enabled = false
            };
            scriptsItem.DropDownItems.Add(noScripts);
        }
        else
        {
            foreach (var script in _config.Scripts)
            {
                var item = new ToolStripMenuItem(script.Name);
                var captured = script;
                item.Click += (_, _) => ScriptClicked?.Invoke(this, captured);
                scriptsItem.DropDownItems.Add(item);
            }
        }

        return scriptsItem;
    }

    private void OnSaveSession(object? sender, EventArgs e) =>
        SaveSessionClicked?.Invoke(this, e);

    private void OnRestoreSession(object? sender, EventArgs e) =>
        RestoreSessionClicked?.Invoke(this, e);

    private void OnSettings(object? sender, EventArgs e) =>
        SettingsClicked?.Invoke(this, e);

    private void OnExit(object? sender, EventArgs e) =>
        ExitClicked?.Invoke(this, e);
}
