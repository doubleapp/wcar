namespace Wcar.UI;

partial class SettingsForm
{
    private System.ComponentModel.IContainer components = null;

    // Auto-Save group
    private GroupBox grpAutoSave;
    private CheckBox chkAutoSaveEnabled;
    private Label lblInterval;
    private NumericUpDown nudInterval;
    private Label lblMinutes;

    // Tracked Apps group
    private GroupBox grpTrackedApps;
    private ListView lstTrackedApps;
    private ColumnHeader colAppEnabled;
    private ColumnHeader colAppName;
    private ColumnHeader colAppLaunch;
    private Button btnAddApp;
    private Button btnRemoveApp;
    private Button btnToggleLaunch;

    // Scripts group
    private GroupBox grpScripts;
    private ListBox lstScripts;
    private Button btnAddScript;
    private Button btnRemoveScript;
    private Button btnEditScript;

    // Startup group
    private GroupBox grpStartup;
    private CheckBox chkAutoStart;
    private CheckBox chkAutoRestore;

    // Buttons
    private Button btnSave;
    private Button btnCancel;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        Text = "WCAR Settings";
        ClientSize = new Size(550, 620);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        // === Auto-Save Group ===
        grpAutoSave = new GroupBox
        {
            Text = "Auto-Save", Location = new Point(12, 12),
            Size = new Size(520, 60)
        };

        chkAutoSaveEnabled = new CheckBox
        {
            Text = "Enable auto-save", Location = new Point(12, 25), AutoSize = true
        };

        lblInterval = new Label
        {
            Text = "Interval:", Location = new Point(170, 27), AutoSize = true
        };

        nudInterval = new NumericUpDown
        {
            Location = new Point(230, 24), Size = new Size(60, 23),
            Minimum = 1, Maximum = 1440, Value = 5
        };

        lblMinutes = new Label
        {
            Text = "minutes", Location = new Point(296, 27), AutoSize = true
        };

        grpAutoSave.Controls.AddRange(new Control[] {
            chkAutoSaveEnabled, lblInterval, nudInterval, lblMinutes
        });

        // === Tracked Apps Group ===
        grpTrackedApps = new GroupBox
        {
            Text = "Tracked Apps", Location = new Point(12, 80),
            Size = new Size(520, 185)
        };

        lstTrackedApps = new ListView
        {
            Location = new Point(12, 25),
            Size = new Size(390, 148),
            View = View.Details,
            FullRowSelect = true,
            CheckBoxes = true,
            GridLines = false,
            MultiSelect = false
        };

        colAppEnabled = new ColumnHeader { Text = "", Width = 30 };
        colAppName = new ColumnHeader { Text = "App Name", Width = 220 };
        colAppLaunch = new ColumnHeader { Text = "Launch", Width = 130 };
        lstTrackedApps.Columns.AddRange(new[] { colAppEnabled, colAppName, colAppLaunch });

        btnAddApp = new Button
        {
            Text = "Add App...", Location = new Point(412, 25), Size = new Size(95, 28)
        };

        btnRemoveApp = new Button
        {
            Text = "Remove", Location = new Point(412, 60), Size = new Size(95, 28)
        };

        btnToggleLaunch = new Button
        {
            Text = "Toggle Launch", Location = new Point(412, 95), Size = new Size(95, 42)
        };

        grpTrackedApps.Controls.AddRange(new Control[] {
            lstTrackedApps, btnAddApp, btnRemoveApp, btnToggleLaunch
        });

        // === Scripts Group ===
        grpScripts = new GroupBox
        {
            Text = "Startup Scripts", Location = new Point(12, 274),
            Size = new Size(520, 150)
        };

        lstScripts = new ListBox
        {
            Location = new Point(12, 25), Size = new Size(390, 112)
        };

        btnAddScript = new Button
        {
            Text = "Add...", Location = new Point(412, 25), Size = new Size(95, 28)
        };

        btnEditScript = new Button
        {
            Text = "Edit...", Location = new Point(412, 60), Size = new Size(95, 28)
        };

        btnRemoveScript = new Button
        {
            Text = "Remove", Location = new Point(412, 95), Size = new Size(95, 28)
        };

        grpScripts.Controls.AddRange(new Control[] {
            lstScripts, btnAddScript, btnEditScript, btnRemoveScript
        });

        // === Startup Group ===
        grpStartup = new GroupBox
        {
            Text = "Startup", Location = new Point(12, 432),
            Size = new Size(520, 80)
        };

        chkAutoStart = new CheckBox
        {
            Text = "Start WCAR with Windows", Location = new Point(12, 25), AutoSize = true
        };

        chkAutoRestore = new CheckBox
        {
            Text = "Auto-restore session on startup", Location = new Point(12, 52), AutoSize = true
        };

        grpStartup.Controls.AddRange(new Control[] { chkAutoStart, chkAutoRestore });

        // === Save/Cancel Buttons ===
        btnSave = new Button
        {
            Text = "Save", Location = new Point(360, 528),
            Size = new Size(80, 32), DialogResult = DialogResult.OK
        };

        btnCancel = new Button
        {
            Text = "Cancel", Location = new Point(450, 528),
            Size = new Size(80, 32), DialogResult = DialogResult.Cancel
        };

        AcceptButton = btnSave;
        CancelButton = btnCancel;

        Controls.AddRange(new Control[] {
            grpAutoSave, grpTrackedApps, grpScripts, grpStartup,
            btnSave, btnCancel
        });
    }
}
