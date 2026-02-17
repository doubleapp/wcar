using Wcar.Config;
using Wcar.Session;

namespace Wcar.UI;

public partial class AppSearchDialog : Form
{
    private List<DiscoveredApp> _allApps = new();
    private List<DiscoveredApp> _filteredApps = new();

    public TrackedApp? SelectedApp { get; private set; }

    public AppSearchDialog()
    {
        InitializeComponent();
        WireEvents();
        LoadAppsAsync();
    }

    private void WireEvents()
    {
        txtSearch.TextChanged += OnSearchChanged;
        tabSource.SelectedIndexChanged += OnTabChanged;
        lstResults.DoubleClick += OnAdd;
        btnAdd.Click += OnAdd;
    }

    private void LoadAppsAsync()
    {
        lblStatus.Text = "Scanning...";
        btnAdd.Enabled = false;

        Task.Run(() =>
        {
            List<DiscoveredApp> installed = new();
            List<DiscoveredApp> running = new();

            try { installed = new StartMenuScanner().Scan().ToList(); } catch { }
            try { running = new RunningProcessScanner().Scan().ToList(); } catch { }

            var all = AppDiscoveryService.FilterAndMerge(installed, running);

            Invoke(() =>
            {
                _allApps = all;
                lblStatus.Text = $"{all.Count} apps found";
                ApplyFilter();
            });
        });
    }

    private void OnSearchChanged(object? sender, EventArgs e) => ApplyFilter();
    private void OnTabChanged(object? sender, EventArgs e) => ApplyFilter();

    private void ApplyFilter()
    {
        var query = txtSearch.Text;
        var source = tabSource.SelectedIndex switch
        {
            1 => AppSource.StartMenu,
            2 => AppSource.RunningProcess,
            _ => (AppSource?)null
        };

        _filteredApps = _allApps
            .Where(a => source == null || a.Source == source)
            .Where(a => string.IsNullOrWhiteSpace(query) ||
                        a.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        a.ProcessName.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(a => a.DisplayName)
            .ToList();

        lstResults.Items.Clear();
        foreach (var app in _filteredApps)
        {
            var item = new ListViewItem(app.DisplayName);
            item.SubItems.Add(app.ProcessName);
            item.SubItems.Add(app.ExecutablePath ?? "");
            lstResults.Items.Add(item);
        }

        btnAdd.Enabled = false;
        lstResults.SelectedIndexChanged += (_, _) =>
            btnAdd.Enabled = lstResults.SelectedItems.Count > 0;
    }

    private void OnAdd(object? sender, EventArgs e)
    {
        if (lstResults.SelectedIndices.Count == 0) return;

        var idx = lstResults.SelectedIndices[0];
        if (idx < 0 || idx >= _filteredApps.Count) return;

        SelectedApp = _filteredApps[idx].ToTrackedApp();
        DialogResult = DialogResult.OK;
        Close();
    }
}
