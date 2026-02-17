using Wcar.Session;

namespace Wcar.UI;

/// <summary>
/// Shown when monitor configuration has changed since the session was saved.
/// Lets the user map saved monitors to current monitors before restoring.
/// </summary>
public partial class ScreenMappingDialog : Form
{
    private readonly IReadOnlyList<MonitorInfo> _savedMonitors;
    private readonly IReadOnlyList<MonitorInfo> _currentMonitors;
    private readonly string _screenshotDir;
    private readonly ComboBox[] _mappingCombos;

    /// <summary>Result: savedIndex → currentIndex mapping. Null if user cancelled.</summary>
    public int[]? Mapping { get; private set; }

    public ScreenMappingDialog(
        IReadOnlyList<MonitorInfo> savedMonitors,
        IReadOnlyList<MonitorInfo> currentMonitors,
        string screenshotDir)
    {
        _savedMonitors = savedMonitors;
        _currentMonitors = currentMonitors;
        _screenshotDir = screenshotDir;
        _mappingCombos = new ComboBox[savedMonitors.Count];

        InitializeComponent();
        BuildMappingRows();
        PopulateWithAutoMap();
    }

    private void BuildMappingRows()
    {
        int y = monitorPanel.Top + 10;

        for (int s = 0; s < _savedMonitors.Count; s++)
        {
            var saved = _savedMonitors[s];
            var savedIdx = s;

            var lbl = new Label
            {
                Text = $"Saved Monitor {s + 1} ({saved.Width}x{saved.Height})",
                Location = new Point(12, y),
                AutoSize = true
            };

            var arrow = new Label
            {
                Text = "→",
                Location = new Point(260, y),
                AutoSize = true
            };

            var combo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(290, y - 3),
                Size = new Size(200, 23)
            };

            for (int c = 0; c < _currentMonitors.Count; c++)
            {
                var cur = _currentMonitors[c];
                var primary = cur.IsPrimary ? " (Primary)" : "";
                combo.Items.Add($"Monitor {c + 1} ({cur.Width}x{cur.Height}){primary}");
            }

            _mappingCombos[savedIdx] = combo;
            mappingPanel.Controls.AddRange(new Control[] { lbl, arrow, combo });

            y += 35;
        }

        mappingPanel.Height = Math.Max(40, y + 10);
    }

    private void PopulateWithAutoMap()
    {
        var autoMap = ScreenMapper.AutoMap(_savedMonitors, _currentMonitors);
        for (int i = 0; i < _mappingCombos.Length; i++)
        {
            var target = i < autoMap.Length ? autoMap[i] : 0;
            if (target < _mappingCombos[i].Items.Count)
                _mappingCombos[i].SelectedIndex = target;
            else if (_mappingCombos[i].Items.Count > 0)
                _mappingCombos[i].SelectedIndex = 0;
        }
    }

    private void OnAutoMap(object? sender, EventArgs e) => PopulateWithAutoMap();

    private void OnApply(object? sender, EventArgs e)
    {
        Mapping = _mappingCombos.Select(c => Math.Max(0, c.SelectedIndex)).ToArray();
        DialogResult = DialogResult.OK;
        Close();
    }

    private void OnCancel(object? sender, EventArgs e)
    {
        Mapping = null;
        DialogResult = DialogResult.Cancel;
        Close();
    }
}
