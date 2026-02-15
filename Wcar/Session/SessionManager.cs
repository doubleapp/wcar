using System.Text.Json;
using Wcar.Config;

namespace Wcar.Session;

public class SessionManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly ConfigManager _configManager;
    private readonly string _sessionPath;
    private readonly string _prevSessionPath;
    private readonly object _lock = new();
    private System.Threading.Timer? _autoSaveTimer;

    public SessionManager(ConfigManager configManager)
    {
        _configManager = configManager;
        _sessionPath = Path.Combine(configManager.DataDir, "session.json");
        _prevSessionPath = Path.Combine(configManager.DataDir, "session.prev.json");
    }

    public string SessionPath => _sessionPath;

    public SessionSnapshot SaveSession()
    {
        lock (_lock)
        {
            var config = _configManager.Load();
            var enumerator = new WindowEnumerator(config.TrackedApps);
            var snapshot = enumerator.CaptureSession();

            // Backup current session
            if (File.Exists(_sessionPath))
            {
                File.Copy(_sessionPath, _prevSessionPath, overwrite: true);
            }

            // Write atomically
            var json = JsonSerializer.Serialize(snapshot, JsonOptions);
            var tmpPath = _sessionPath + ".tmp";
            File.WriteAllText(tmpPath, json);
            File.Move(tmpPath, _sessionPath, overwrite: true);

            return snapshot;
        }
    }

    public SessionSnapshot? LoadSession()
    {
        lock (_lock)
        {
            if (!File.Exists(_sessionPath))
                return null;

            try
            {
                var json = File.ReadAllText(_sessionPath);
                return JsonSerializer.Deserialize<SessionSnapshot>(json, JsonOptions);
            }
            catch (JsonException)
            {
                HandleCorruptSession();
                return null;
            }
        }
    }

    public void RestoreSession(NotifyIcon notifyIcon)
    {
        var snapshot = LoadSession();
        if (snapshot == null)
        {
            UI.NotificationHelper.ShowWarning(notifyIcon, "No saved session found.");
            return;
        }

        var config = _configManager.Load();
        var restorer = new WindowRestorer(config.TrackedApps);

        try
        {
            var results = restorer.Restore(snapshot);

            foreach (var msg in results.Warnings)
                UI.NotificationHelper.ShowWarning(notifyIcon, msg);

            if (results.Errors.Count == 0 && results.Warnings.Count == 0)
                UI.NotificationHelper.ShowInfo(notifyIcon, "Session restored successfully.");
        }
        catch (Exception ex)
        {
            UI.NotificationHelper.ShowError(notifyIcon, $"Restore failed: {ex.Message}");
        }
    }

    public void StartAutoSave(int intervalMinutes)
    {
        StopAutoSave();

        var interval = TimeSpan.FromMinutes(intervalMinutes);
        _autoSaveTimer = new System.Threading.Timer(_ =>
        {
            try { SaveSession(); }
            catch { /* auto-save failures are non-critical */ }
        }, null, interval, interval);
    }

    public void StopAutoSave()
    {
        _autoSaveTimer?.Dispose();
        _autoSaveTimer = null;
    }

    private void HandleCorruptSession()
    {
        var corruptPath = _sessionPath + ".corrupt.json";
        if (File.Exists(corruptPath))
            File.Delete(corruptPath);
        File.Move(_sessionPath, corruptPath);
    }
}
