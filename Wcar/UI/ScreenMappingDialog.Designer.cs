namespace Wcar.UI;

partial class ScreenMappingDialog
{
    private System.ComponentModel.IContainer components = null;

    private Label lblInfo;
    private Panel monitorPanel;
    private Panel mappingPanel;
    private Button btnAutoMap;
    private Button btnApply;
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

        Text = "Screen Configuration Changed";
        ClientSize = new Size(560, 400);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        lblInfo = new Label
        {
            Text = "Your monitors have changed since the session was saved.\r\nMap your saved screens to current screens.",
            Location = new Point(12, 12),
            Size = new Size(530, 40),
            AutoSize = false
        };

        monitorPanel = new Panel
        {
            Location = new Point(12, 60),
            Size = new Size(530, 20),
            BorderStyle = BorderStyle.None
        };

        mappingPanel = new Panel
        {
            Location = new Point(12, 80),
            Size = new Size(530, 260),
            AutoScroll = true,
            BorderStyle = BorderStyle.FixedSingle
        };

        btnAutoMap = new Button
        {
            Text = "Auto-Map",
            Location = new Point(12, 350),
            Size = new Size(90, 30)
        };
        btnAutoMap.Click += OnAutoMap;

        btnApply = new Button
        {
            Text = "Apply",
            Location = new Point(370, 350),
            Size = new Size(80, 30),
            DialogResult = DialogResult.OK
        };
        btnApply.Click += OnApply;

        btnCancel = new Button
        {
            Text = "Cancel Restore",
            Location = new Point(460, 350),
            Size = new Size(90, 30),
            DialogResult = DialogResult.Cancel
        };
        btnCancel.Click += OnCancel;

        AcceptButton = btnApply;
        CancelButton = btnCancel;

        Controls.AddRange(new Control[] { lblInfo, monitorPanel, mappingPanel, btnAutoMap, btnApply, btnCancel });
    }
}
