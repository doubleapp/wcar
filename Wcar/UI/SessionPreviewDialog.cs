using Wcar.Session;

namespace Wcar.UI;

/// <summary>
/// Shows screenshot thumbnails of the saved session side by side with monitor labels.
/// </summary>
public partial class SessionPreviewDialog : Form
{
    private readonly string _screenshotDir;

    public SessionPreviewDialog(string screenshotDir)
    {
        _screenshotDir = screenshotDir;
        InitializeComponent();
        LoadScreenshots();
    }

    private void LoadScreenshots()
    {
        screenshotPanel.Controls.Clear();
        int x = 10;

        for (int i = 0; ; i++)
        {
            var path = ScreenshotHelper.GetScreenshotPath(_screenshotDir, i);
            if (!File.Exists(path)) break;

            try
            {
                var img = Image.FromFile(path);
                var thumb = img.GetThumbnailImage(280, 160, null, IntPtr.Zero);

                var box = new PictureBox
                {
                    Image = thumb,
                    Size = new Size(280, 160),
                    Location = new Point(x, 30),
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    BorderStyle = BorderStyle.FixedSingle
                };

                var lbl = new Label
                {
                    Text = $"Monitor {i + 1}",
                    Location = new Point(x, 10),
                    AutoSize = true
                };

                screenshotPanel.Controls.Add(box);
                screenshotPanel.Controls.Add(lbl);
                x += 300;
            }
            catch
            {
                // Skip unavailable screenshots
            }
        }

        if (screenshotPanel.Controls.Count == 0)
        {
            screenshotPanel.Controls.Add(new Label
            {
                Text = "No screenshots available.",
                Location = new Point(10, 30),
                AutoSize = true
            });
        }
    }
}
