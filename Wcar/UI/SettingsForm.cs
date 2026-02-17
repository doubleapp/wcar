using Wcar.Config;
using Wcar.Scripts;
using Wcar.Session;

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
        chkAutoSaveEnabled.Checked = _config.AutoSaveEnabled;
        nudInterval.Value = Math.Clamp(_config.AutoSaveIntervalMinutes, 1, 1440);

        RefreshTrackedAppsList();
        RefreshScriptsList();

        chkAutoStart.Checked = _config.AutoStartEnabled && _startupManager.IsAutoStartRegistered();
        chkAutoRestore.Checked = _config.AutoRestoreEnabled;
    }

    private void WireEvents()
    {
        btnSave.Click += OnSave;
        btnAddScript.Click += OnAddScript;
        btnEditScript.Click += OnEditScript;
        btnRemoveScript.Click += OnRemoveScript;
        btnAddApp.Click += OnAddApp;
        btnRemoveApp.Click += OnRemoveApp;
        btnToggleLaunch.Click += OnToggleLaunch;
        lstTrackedApps.ItemChecked += OnAppCheckedChanged;
    }

    // ── Tracked Apps ─────────────────────────────────────────────────────────

    private void RefreshTrackedAppsList()
    {
        lstTrackedApps.Items.Clear();
        foreach (var app in _config.TrackedApps)
        {
            var item = new ListViewItem { Checked = app.Enabled, Tag = app };
            item.Text = app.DisplayName;
            item.SubItems.Add(app.Launch.ToString());
            lstTrackedApps.Items.Add(item);
        }
    }

    private void OnAppCheckedChanged(object? sender, ItemCheckedEventArgs e)
    {
        if (e.Item.Tag is TrackedApp app)
            app.Enabled = e.Item.Checked;
    }

    private void OnAddApp(object? sender, EventArgs e)
    {
        using var dialog = new AppSearchDialog();
        if (dialog.ShowDialog(this) == DialogResult.OK && dialog.SelectedApp != null)
        {
            _config.TrackedApps.Add(dialog.SelectedApp);
            RefreshTrackedAppsList();
        }
    }

    private void OnRemoveApp(object? sender, EventArgs e)
    {
        if (lstTrackedApps.SelectedItems.Count == 0) return;

        var idx = lstTrackedApps.SelectedItems[0].Index;
        if (idx >= 0 && idx < _config.TrackedApps.Count)
        {
            _config.TrackedApps.RemoveAt(idx);
            RefreshTrackedAppsList();
        }
    }

    private void OnToggleLaunch(object? sender, EventArgs e)
    {
        if (lstTrackedApps.SelectedItems.Count == 0) return;

        var idx = lstTrackedApps.SelectedItems[0].Index;
        if (idx >= 0 && idx < _config.TrackedApps.Count)
        {
            var app = _config.TrackedApps[idx];
            app.Launch = app.Launch == LaunchStrategy.LaunchOnce
                ? LaunchStrategy.LaunchPerWindow
                : LaunchStrategy.LaunchOnce;
            RefreshTrackedAppsList();
        }
    }

    // ── Save ─────────────────────────────────────────────────────────────────

    private void OnSave(object? sender, EventArgs e)
    {
        _config.AutoSaveEnabled = chkAutoSaveEnabled.Checked;
        _config.AutoSaveIntervalMinutes = (int)nudInterval.Value;
        _config.AutoRestoreEnabled = chkAutoRestore.Checked;

        HandleAutoStartToggle();

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

    // ── Scripts ───────────────────────────────────────────────────────────────

    private void OnAddScript(object? sender, EventArgs e)
    {
        var name = PromptInput("Add Script", "Script name:");
        if (name == null) return;

        var command = PromptInput("Add Script", "Command:");
        if (command == null) return;

        var shell = PromptShellSelection("Add Script");
        if (shell == null) return;

        var description = PromptInput("Add Script", "Description (optional):", "") ?? "";

        var manager = new ScriptManager(_configManager);
        if (manager.AddScript(name, command, shell.Value, description))
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

        var script = _config.Scripts[lstScripts.SelectedIndex];
        var newCommand = PromptInput("Edit Script", "Command:", script.Command);
        if (newCommand == null) return;

        var shell = PromptShellSelection("Edit Script", script.Shell);
        if (shell == null) return;

        var description = PromptInput("Edit Script", "Description:", script.Description) ?? "";

        var manager = new ScriptManager(_configManager);
        manager.EditScript(script.Name, newCommand, shell, description);
        _config = _configManager.Load();
        RefreshScriptsList();
    }

    private void OnRemoveScript(object? sender, EventArgs e)
    {
        if (lstScripts.SelectedIndex < 0) return;

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
        {
            var desc = string.IsNullOrEmpty(s.Description) ? "" : $" \u2014 {s.Description}";
            lstScripts.Items.Add($"[{s.Shell}] {s.Name}: {s.Command}{desc}");
        }
    }

    private static ScriptShell? PromptShellSelection(string title,
        ScriptShell current = ScriptShell.PowerShell)
    {
        using var form = new Form
        {
            Text = title,
            ClientSize = new Size(350, 110),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var lbl = new Label { Text = "Shell:", Location = new Point(12, 12), AutoSize = true };
        var combo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(12, 35),
            Size = new Size(320, 23)
        };
        foreach (var shell in Enum.GetValues<ScriptShell>())
            combo.Items.Add(shell.ToString());
        combo.SelectedItem = current.ToString();

        var ok = new Button
        {
            Text = "OK", DialogResult = DialogResult.OK,
            Location = new Point(170, 70), Size = new Size(75, 28)
        };
        var cancel = new Button
        {
            Text = "Cancel", DialogResult = DialogResult.Cancel,
            Location = new Point(255, 70), Size = new Size(75, 28)
        };

        form.AcceptButton = ok;
        form.CancelButton = cancel;
        form.Controls.AddRange(new Control[] { lbl, combo, ok, cancel });

        if (form.ShowDialog() != DialogResult.OK) return null;
        return Enum.Parse<ScriptShell>((string)combo.SelectedItem!);
    }

    private static string? PromptInput(string title, string label, string defaultValue = "")
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
