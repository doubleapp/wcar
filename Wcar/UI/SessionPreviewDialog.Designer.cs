namespace Wcar.UI;

partial class SessionPreviewDialog
{
    private System.ComponentModel.IContainer components = null;

    private Panel screenshotPanel;
    private Button btnClose;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        Text = "Saved Session Preview";
        ClientSize = new Size(920, 280);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        screenshotPanel = new Panel
        {
            Location = new Point(12, 12),
            Size = new Size(890, 210),
            AutoScroll = true,
            BorderStyle = BorderStyle.FixedSingle
        };

        btnClose = new Button
        {
            Text = "Close",
            Location = new Point(420, 235),
            Size = new Size(80, 28),
            DialogResult = DialogResult.OK
        };

        AcceptButton = btnClose;
        Controls.AddRange(new Control[] { screenshotPanel, btnClose });
    }
}
