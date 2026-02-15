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
    private CheckBox chkChrome;
    private CheckBox chkVSCode;
    private CheckBox chkCMD;
    private CheckBox chkPowerShell;
    private CheckBox chkExplorer;
    private CheckBox chkDocker;

    // Scripts group
    private GroupBox grpScripts;
    private ListBox lstScripts;
    private Button btnAddScript;
    private Button btnRemoveScript;
    private Button btnEditScript;

    // Startup group
    private GroupBox grpStartup;
    private CheckBox chkAutoStart;
    private CheckBox chkDiskCheck;
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
        ClientSize = new Size(480, 560);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        // === Auto-Save Group ===
        grpAutoSave = new GroupBox
        {
            Text = "Auto-Save", Location = new Point(12, 12),
            Size = new Size(450, 60)
        };

        chkAutoSaveEnabled = new CheckBox
        {
            Text = "Enable auto-save", Location = new Point(12, 25),
            AutoSize = true
        };

        lblInterval = new Label
        {
            Text = "Interval:", Location = new Point(170, 27),
            AutoSize = true
        };

        nudInterval = new NumericUpDown
        {
            Location = new Point(230, 24), Size = new Size(60, 23),
            Minimum = 1, Maximum = 1440, Value = 5
        };

        lblMinutes = new Label
        {
            Text = "minutes", Location = new Point(296, 27),
            AutoSize = true
        };

        grpAutoSave.Controls.AddRange(new Control[] {
            chkAutoSaveEnabled, lblInterval, nudInterval, lblMinutes
        });

        // === Tracked Apps Group ===
        grpTrackedApps = new GroupBox
        {
            Text = "Tracked Apps", Location = new Point(12, 80),
            Size = new Size(450, 100)
        };

        chkChrome = new CheckBox
        {
            Text = "Chrome", Location = new Point(12, 25), AutoSize = true
        };
        chkVSCode = new CheckBox
        {
            Text = "VS Code", Location = new Point(160, 25), AutoSize = true
        };
        chkCMD = new CheckBox
        {
            Text = "CMD", Location = new Point(310, 25), AutoSize = true
        };
        chkPowerShell = new CheckBox
        {
            Text = "PowerShell", Location = new Point(12, 55), AutoSize = true
        };
        chkExplorer = new CheckBox
        {
            Text = "Explorer", Location = new Point(160, 55), AutoSize = true
        };
        chkDocker = new CheckBox
        {
            Text = "Docker Desktop", Location = new Point(310, 55), AutoSize = true
        };

        grpTrackedApps.Controls.AddRange(new Control[] {
            chkChrome, chkVSCode, chkCMD, chkPowerShell, chkExplorer, chkDocker
        });

        // === Scripts Group ===
        grpScripts = new GroupBox
        {
            Text = "Startup Scripts", Location = new Point(12, 188),
            Size = new Size(450, 150)
        };

        lstScripts = new ListBox
        {
            Location = new Point(12, 25), Size = new Size(320, 110)
        };

        btnAddScript = new Button
        {
            Text = "Add...", Location = new Point(344, 25), Size = new Size(90, 28)
        };

        btnEditScript = new Button
        {
            Text = "Edit...", Location = new Point(344, 60), Size = new Size(90, 28)
        };

        btnRemoveScript = new Button
        {
            Text = "Remove", Location = new Point(344, 95), Size = new Size(90, 28)
        };

        grpScripts.Controls.AddRange(new Control[] {
            lstScripts, btnAddScript, btnEditScript, btnRemoveScript
        });

        // === Startup Group ===
        grpStartup = new GroupBox
        {
            Text = "Startup", Location = new Point(12, 346),
            Size = new Size(450, 110)
        };

        chkAutoStart = new CheckBox
        {
            Text = "Start WCAR with Windows", Location = new Point(12, 25),
            AutoSize = true
        };

        chkDiskCheck = new CheckBox
        {
            Text = "Run disk space check at logon", Location = new Point(12, 55),
            AutoSize = true
        };

        chkAutoRestore = new CheckBox
        {
            Text = "Auto-restore session on startup", Location = new Point(12, 85),
            AutoSize = true
        };

        grpStartup.Controls.AddRange(new Control[] {
            chkAutoStart, chkDiskCheck, chkAutoRestore
        });

        // === Save/Cancel Buttons ===
        btnSave = new Button
        {
            Text = "Save", Location = new Point(290, 468),
            Size = new Size(80, 32), DialogResult = DialogResult.OK
        };

        btnCancel = new Button
        {
            Text = "Cancel", Location = new Point(380, 468),
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
