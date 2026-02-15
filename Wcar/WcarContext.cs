using Wcar.Config;
using Wcar.Session;
using Wcar.UI;

namespace Wcar;

public class WcarContext : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ConfigManager _configManager;
    private readonly TrayMenuBuilder _menuBuilder;
    private readonly SessionManager _sessionManager;
    private AppConfig _config;

    public WcarContext(ConfigManager configManager)
    {
        _configManager = configManager;
        _config = _configManager.Load();
        _sessionManager = new SessionManager(_configManager);

        _notifyIcon = new NotifyIcon
        {
            Text = "WCAR - Window Configuration Auto Restorer",
            Icon = LoadAppIcon(),
            Visible = true
        };

        _menuBuilder = new TrayMenuBuilder(_config, _notifyIcon);
        WireMenuEvents();
        _notifyIcon.ContextMenuStrip = _menuBuilder.Build();

        // Start auto-save if enabled
        if (_config.AutoSaveEnabled)
            _sessionManager.StartAutoSave(_config.AutoSaveIntervalMinutes);

        // Auto-restore if enabled (delay 10s to let system settle)
        if (_config.AutoRestoreEnabled)
        {
            var ctx = SynchronizationContext.Current;
            Task.Delay(TimeSpan.FromSeconds(10)).ContinueWith(_ =>
            {
                if (ctx != null)
                    ctx.Post(__ => _sessionManager.RestoreSession(_notifyIcon), null);
                else
                    _sessionManager.RestoreSession(_notifyIcon);
            });
        }
    }

    public NotifyIcon NotifyIcon => _notifyIcon;
    public ConfigManager ConfigManager => _configManager;
    public SessionManager SessionManager => _sessionManager;
    public AppConfig Config => _config;

    public void ReloadConfig()
    {
        _config = _configManager.Load();
        _menuBuilder.RefreshMenu(_config);

        // Restart auto-save with new settings
        _sessionManager.StopAutoSave();
        if (_config.AutoSaveEnabled)
            _sessionManager.StartAutoSave(_config.AutoSaveIntervalMinutes);
    }

    private void WireMenuEvents()
    {
        _menuBuilder.SaveSessionClicked += OnSaveSession;
        _menuBuilder.RestoreSessionClicked += OnRestoreSession;
        _menuBuilder.SettingsClicked += OnSettings;
        _menuBuilder.ExitClicked += OnExit;
        _menuBuilder.ScriptClicked += OnScriptClicked;
    }

    private void OnSaveSession(object? sender, EventArgs e)
    {
        try
        {
            var snapshot = _sessionManager.SaveSession();
            NotificationHelper.ShowInfo(_notifyIcon,
                $"Session saved: {snapshot.Windows.Count} windows captured.");
        }
        catch (Exception ex)
        {
            NotificationHelper.ShowError(_notifyIcon, $"Save failed: {ex.Message}");
        }
    }

    private void OnRestoreSession(object? sender, EventArgs e)
    {
        _sessionManager.RestoreSession(_notifyIcon);
    }

    private void OnSettings(object? sender, EventArgs e)
    {
        using var form = new SettingsForm(_configManager);
        if (form.ShowDialog() == DialogResult.OK)
        {
            ReloadConfig();
        }
    }

    private void OnScriptClicked(object? sender, Config.ScriptEntry script)
    {
        if (!Scripts.ScriptRunner.Run(script.Command))
        {
            NotificationHelper.ShowError(_notifyIcon,
                $"Failed to run script: {script.Name}");
        }
    }

    private void OnExit(object? sender, EventArgs e)
    {
        _sessionManager.StopAutoSave();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        Application.Exit();
    }

    private static Icon LoadAppIcon()
    {
        var icoPath = Path.Combine(AppContext.BaseDirectory, "wcar.ico");
        if (File.Exists(icoPath))
            return new Icon(icoPath);
        return SystemIcons.Application;
    }
}
