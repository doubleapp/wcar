namespace Wcar.UI;

partial class AppSearchDialog
{
    private System.ComponentModel.IContainer components = null;

    private TabControl tabSource;
    private TabPage tabAll;
    private TabPage tabInstalled;
    private TabPage tabRunning;
    private TextBox txtSearch;
    private ListView lstResults;
    private ColumnHeader colName;
    private ColumnHeader colProcess;
    private ColumnHeader colPath;
    private Label lblStatus;
    private Button btnAdd;
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

        Text = "Add App";
        ClientSize = new Size(600, 450);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;

        // Search box
        txtSearch = new TextBox
        {
            Location = new Point(12, 12),
            Size = new Size(570, 23),
            PlaceholderText = "Search by name, process, or path..."
        };

        // Source tabs
        tabSource = new TabControl
        {
            Location = new Point(12, 44),
            Size = new Size(570, 30)
        };

        tabAll = new TabPage("All");
        tabInstalled = new TabPage("Installed Apps");
        tabRunning = new TabPage("Running Now");
        tabSource.Controls.Add(tabAll);
        tabSource.Controls.Add(tabInstalled);
        tabSource.Controls.Add(tabRunning);
        tabSource.Appearance = TabAppearance.FlatButtons;

        // Results list
        lstResults = new ListView
        {
            Location = new Point(12, 80),
            Size = new Size(570, 310),
            View = View.Details,
            FullRowSelect = true,
            GridLines = false,
            MultiSelect = false
        };

        colName = new ColumnHeader { Text = "Display Name", Width = 200 };
        colProcess = new ColumnHeader { Text = "Process", Width = 120 };
        colPath = new ColumnHeader { Text = "Path", Width = 240 };
        lstResults.Columns.AddRange(new[] { colName, colProcess, colPath });

        // Status label
        lblStatus = new Label
        {
            Location = new Point(12, 400),
            Size = new Size(300, 20),
            Text = "Loading..."
        };

        // Buttons
        btnAdd = new Button
        {
            Text = "Add",
            Location = new Point(418, 396),
            Size = new Size(75, 28),
            DialogResult = DialogResult.OK,
            Enabled = false
        };

        btnCancel = new Button
        {
            Text = "Cancel",
            Location = new Point(503, 396),
            Size = new Size(75, 28),
            DialogResult = DialogResult.Cancel
        };

        AcceptButton = btnAdd;
        CancelButton = btnCancel;

        Controls.AddRange(new Control[] { txtSearch, tabSource, lstResults, lblStatus, btnAdd, btnCancel });
    }
}
