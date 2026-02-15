using Wcar.Config;
using Wcar.Scripts;

namespace Wcar.UI;

public partial class SettingsForm : Form
{
    private readonly ConfigManager _configManager;
    private readonly StartupTaskManager _startupManager;
    private AppConfig _config;

    public SettingsForm(ConfigManager configManager)
    {
        _configManager = configManager;
        _startupManager = new StartupTaskManager();
        _config = _configManager.Load();

        InitializeComponent();
        LoadSettings();
        WireEvents();
    }

    private void LoadSettings()
    {
        // Auto-save
        chkAutoSaveEnabled.Checked = _config.AutoSaveEnabled;
        nudInterval.Value = Math.Clamp(_config.AutoSaveIntervalMinutes, 1, 1440);

        // Tracked apps
        chkChrome.Checked = _config.TrackedApps.GetValueOrDefault("Chrome", true);
        chkVSCode.Checked = _config.TrackedApps.GetValueOrDefault("VSCode", true);
        chkCMD.Checked = _config.TrackedApps.GetValueOrDefault("CMD", true);
        chkPowerShell.Checked = _config.TrackedApps.GetValueOrDefault("PowerShell", true);
        chkExplorer.Checked = _config.TrackedApps.GetValueOrDefault("Explorer", true);
        chkDocker.Checked = _config.TrackedApps.GetValueOrDefault("DockerDesktop", true);

        // Scripts
        RefreshScriptsList();

        // Startup - sync with actual registration state
        chkAutoStart.Checked = _config.AutoStartEnabled
                               && _startupManager.IsAutoStartRegistered();
        chkDiskCheck.Checked = _config.DiskCheckEnabled
                               && _startupManager.IsDiskCheckRegistered();
        chkAutoRestore.Checked = _config.AutoRestoreEnabled;
    }

    private void WireEvents()
    {
        btnSave.Click += OnSave;
        btnAddScript.Click += OnAddScript;
        btnEditScript.Click += OnEditScript;
        btnRemoveScript.Click += OnRemoveScript;
    }

    private void OnSave(object? sender, EventArgs e)
    {
        // Auto-save
        _config.AutoSaveEnabled = chkAutoSaveEnabled.Checked;
        _config.AutoSaveIntervalMinutes = (int)nudInterval.Value;

        // Tracked apps
        _config.TrackedApps["Chrome"] = chkChrome.Checked;
        _config.TrackedApps["VSCode"] = chkVSCode.Checked;
        _config.TrackedApps["CMD"] = chkCMD.Checked;
        _config.TrackedApps["PowerShell"] = chkPowerShell.Checked;
        _config.TrackedApps["Explorer"] = chkExplorer.Checked;
        _config.TrackedApps["DockerDesktop"] = chkDocker.Checked;

        // Auto-restore
        _config.AutoRestoreEnabled = chkAutoRestore.Checked;

        // Startup registrations
        HandleAutoStartToggle();
        HandleDiskCheckToggle();

        _configManager.Save(_config);
        DialogResult = DialogResult.OK;
        Close();
    }

    private void HandleAutoStartToggle()
    {
        var exePath = Application.ExecutablePath;

        if (chkAutoStart.Checked && !_config.AutoStartEnabled)
        {
            if (!_startupManager.RegisterAutoStart(exePath))
            {
                MessageBox.Show("Failed to register auto-start. " +
                    "Try running as administrator.", "WCAR",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                chkAutoStart.Checked = false;
            }
        }
        else if (!chkAutoStart.Checked && _config.AutoStartEnabled)
        {
            _startupManager.UnregisterAutoStart();
        }

        _config.AutoStartEnabled = chkAutoStart.Checked;
    }

    private void HandleDiskCheckToggle()
    {
        if (chkDiskCheck.Checked && !_config.DiskCheckEnabled)
        {
            if (!_startupManager.RegisterDiskCheck())
            {
                var scriptPath = Path.Combine(AppContext.BaseDirectory, "check-disk-space.ps1");
                var msg = File.Exists(scriptPath)
                    ? "Failed to register disk check task."
                    : "check-disk-space.ps1 not found next to the executable.";

                MessageBox.Show(msg, "WCAR",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                chkDiskCheck.Checked = false;
            }
        }
        else if (!chkDiskCheck.Checked && _config.DiskCheckEnabled)
        {
            _startupManager.UnregisterDiskCheck();
        }

        _config.DiskCheckEnabled = chkDiskCheck.Checked;
    }

    private void OnAddScript(object? sender, EventArgs e)
    {
        if (!CheckUacForScripts()) return;

        var name = PromptInput("Add Script", "Script name:");
        if (name == null) return;

        var command = PromptInput("Add Script", "PowerShell command:");
        if (command == null) return;

        var manager = new ScriptManager(_configManager);
        if (manager.AddScript(name, command))
        {
            _config = _configManager.Load();
            RefreshScriptsList();
        }
        else
        {
            MessageBox.Show("Script already exists or invalid input.", "WCAR",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void OnEditScript(object? sender, EventArgs e)
    {
        if (lstScripts.SelectedIndex < 0) return;
        if (!CheckUacForScripts()) return;

        var script = _config.Scripts[lstScripts.SelectedIndex];
        var newCommand = PromptInput("Edit Script", "New command:", script.Command);
        if (newCommand == null) return;

        var manager = new ScriptManager(_configManager);
        manager.EditScript(script.Name, newCommand);
        _config = _configManager.Load();
        RefreshScriptsList();
    }

    private void OnRemoveScript(object? sender, EventArgs e)
    {
        if (lstScripts.SelectedIndex < 0) return;
        if (!CheckUacForScripts()) return;

        var script = _config.Scripts[lstScripts.SelectedIndex];
        var manager = new ScriptManager(_configManager);
        manager.RemoveScript(script.Name);
        _config = _configManager.Load();
        RefreshScriptsList();
    }

    private void RefreshScriptsList()
    {
        lstScripts.Items.Clear();
        foreach (var s in _config.Scripts)
            lstScripts.Items.Add($"{s.Name}: {s.Command}");
    }

    private static bool CheckUacForScripts()
    {
        if (UacHelper.IsElevated()) return true;

        var result = MessageBox.Show(
            "Script management requires administrator privileges.\n" +
            "Would you like to restart WCAR as administrator?",
            "WCAR - Elevation Required",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            UacHelper.RequestElevation(Application.ExecutablePath);
        }
        return false;
    }

    private static string? PromptInput(string title, string label,
        string defaultValue = "")
    {
        using var form = new Form
        {
            Text = title,
            ClientSize = new Size(350, 120),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var lbl = new Label { Text = label, Location = new Point(12, 12), AutoSize = true };
        var txt = new TextBox
        {
            Text = defaultValue,
            Location = new Point(12, 35),
            Size = new Size(320, 23)
        };
        var ok = new Button
        {
            Text = "OK", DialogResult = DialogResult.OK,
            Location = new Point(170, 75), Size = new Size(75, 28)
        };
        var cancel = new Button
        {
            Text = "Cancel", DialogResult = DialogResult.Cancel,
            Location = new Point(255, 75), Size = new Size(75, 28)
        };

        form.AcceptButton = ok;
        form.CancelButton = cancel;
        form.Controls.AddRange(new Control[] { lbl, txt, ok, cancel });

        return form.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(txt.Text)
            ? txt.Text
            : null;
    }
}
