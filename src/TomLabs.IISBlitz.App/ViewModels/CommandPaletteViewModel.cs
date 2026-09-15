using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TomLabs.IISBlitz.App.ViewModels;

/// <summary>One entry of the command palette: a site to jump to, a tab, or an action on the selected site.</summary>
public sealed record PaletteItem(string Group, string Icon, string Title, string Hint, string Shortcut, Action Run);

/// <summary>
/// Ctrl+K palette: fuzzy search across sites, tabs and actions.
/// Items are rebuilt every time the palette opens so they reflect the selected site's state.
/// </summary>
public partial class CommandPaletteViewModel : ObservableObject
{
    private readonly MainWindowViewModel _main;
    private readonly List<PaletteItem> _all = new();

    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private ObservableCollection<PaletteItem> _items = new();

    [ObservableProperty]
    private PaletteItem? _selectedItem;

    private string _query = string.Empty;
    public string Query
    {
        get => _query;
        set
        {
            if (SetProperty(ref _query, value))
                Filter();
        }
    }

    public CommandPaletteViewModel(MainWindowViewModel main)
    {
        _main = main;
    }

    public void Open()
    {
        BuildItems();
        Query = string.Empty;
        Filter();
        IsOpen = true;
    }

    public void Close() => IsOpen = false;

    public void MoveSelection(int delta)
    {
        if (Items.Count == 0) return;
        var index = SelectedItem is null ? -1 : Items.IndexOf(SelectedItem);
        index = Math.Clamp(index + delta, 0, Items.Count - 1);
        SelectedItem = Items[index];
    }

    public void RunSelected()
    {
        var item = SelectedItem ?? Items.FirstOrDefault();
        if (item is null) return;
        Close();
        item.Run();
    }

    private void BuildItems()
    {
        _all.Clear();
        var vm = _main.SiteViewModel;
        var site = vm.SelectedSite;
        var siteName = site?.Name ?? string.Empty;

        if (site != null)
        {
            _all.Add(new PaletteItem("Actions", "fa-solid fa-recycle", "Recycle app pool", site.AppPool, "Ctrl+R", () => vm.RecyclePoolCmd.Execute(null)));
            _all.Add(new PaletteItem("Actions", "fa-solid fa-rotate", "Restart site", "stop + start", "", () => vm.RestartSiteCmd.Execute(null)));
            if (site.IsRunning)
                _all.Add(new PaletteItem("Actions", "fa-solid fa-stop", "Stop site", siteName, "", () => vm.StopWebCmd.Execute(null)));
            else
                _all.Add(new PaletteItem("Actions", "fa-solid fa-play", "Start site", siteName, "", () => vm.StartWebCmd.Execute(null)));
            if (site.IsPoolRunning)
                _all.Add(new PaletteItem("Actions", "fa-solid fa-stop", "Stop app pool", site.AppPool, "", () => vm.StopPoolCmd.Execute(null)));
            else
                _all.Add(new PaletteItem("Actions", "fa-solid fa-play", "Start app pool", site.AppPool, "", () => vm.StartPoolCmd.Execute(null)));
            _all.Add(new PaletteItem("Actions", "fa-solid fa-heart-pulse", "Ping site", site.Url ?? "no binding", "", () => vm.HealthCheckCmd.Execute(null)));
            _all.Add(new PaletteItem("Actions", "fa-solid fa-chart-line", "Run 5 pings", "builds the response time chart", "", () => vm.RunHealthCheckSeriesCmd.Execute(null)));
            _all.Add(new PaletteItem("Actions", "fa-solid fa-globe", "Open in browser", site.Url ?? string.Empty, "", () => vm.BrowseSiteCmd.Execute(null)));
            _all.Add(new PaletteItem("Actions", "fa-solid fa-folder-open", "Open folder", site.PhysicalPath, "", () => vm.OpenWebFolderCmd.Execute(null)));
            _all.Add(new PaletteItem("Actions", "fa-solid fa-copy", "Copy physical path", site.PhysicalPath, "", () => vm.CopyPathCmd.Execute(null)));
            _all.Add(new PaletteItem("Actions", "fa-solid fa-floppy-disk", "Save appsettings.json", siteName, "Ctrl+S", () => vm.SaveAppSettingsCmd.Execute(null)));
            _all.Add(new PaletteItem("Actions", "fa-solid fa-floppy-disk", "Save web.config", siteName, "Ctrl+S", () => vm.SaveWebConfigCmd.Execute(null)));
            _all.Add(new PaletteItem("Actions", "fa-solid fa-calendar", "Load Windows events", "last 24 h", "", () => { _main.SelectedTabIndex = 5; vm.LoadEventLogCmd.Execute(null); }));
        }

        foreach (var s in vm.SiteList)
        {
            var captured = s;
            var state = s.IsRunning ? "running" : "stopped";
            _all.Add(new PaletteItem("Jump to", "fa-solid fa-globe", s.Name, $"{s.AppPool} · {state}", "", () => vm.SelectedSite = captured));
        }

        var tabs = new[] { "Overview", "appsettings.json", "web.config", "Permissions", "Logs", "Events" };
        for (var i = 0; i < tabs.Length; i++)
        {
            var index = i;
            _all.Add(new PaletteItem("Jump to", "fa-solid fa-table-columns", tabs[i], siteName, "", () => _main.SelectedTabIndex = index));
        }

        _all.Add(new PaletteItem("App", "fa-solid fa-arrows-rotate", "Refresh sites", "reload from IIS", "F5", () => vm.RefreshSitesCmd.Execute(null)));
        _all.Add(new PaletteItem("App", "fa-solid fa-circle-half-stroke", "Toggle light / dark", "remembered for next start", "Ctrl+Shift+L", () => vm.ToggleThemeCmd.Execute(null)));

        if (TomLabs.AutoUpdate.Updater.Current is { } updater)
        {
            var build = $"{updater.Build.Version} · {updater.Channel.ToString().ToLowerInvariant()} channel";
            _all.Add(new PaletteItem("App", "fa-solid fa-cloud-arrow-down", "Check for updates", build, "", () => _ = updater.CheckAsync()));
            var other = updater.Channel == TomLabs.AutoUpdate.UpdateChannel.Stable ? TomLabs.AutoUpdate.UpdateChannel.Nightly : TomLabs.AutoUpdate.UpdateChannel.Stable;
            _all.Add(new PaletteItem("App", "fa-solid fa-code-branch", $"Switch to {other.ToString().ToLowerInvariant()} updates",
                other == TomLabs.AutoUpdate.UpdateChannel.Nightly ? "latest commit on master" : "tagged releases only", "", () => updater.Channel = other));
        }
    }

    private void Filter()
    {
        var query = Query.Trim();
        IEnumerable<PaletteItem> result = string.IsNullOrEmpty(query)
            ? _all
            : _all.Select(i => (item: i, score: Score(i, query)))
                  .Where(t => t.score > 0)
                  .OrderByDescending(t => t.score)
                  .Select(t => t.item);

        Items = new ObservableCollection<PaletteItem>(result.Take(12));
        SelectedItem = Items.FirstOrDefault();
    }

    /// <summary>Simple fuzzy score: prefix beats substring beats subsequence; the hint counts half.</summary>
    private static int Score(PaletteItem item, string query)
    {
        var title = Score(item.Title, query);
        var hint = Score(item.Hint, query) / 2;
        return Math.Max(title, hint);
    }

    private static int Score(string text, string query)
    {
        if (text.StartsWith(query, StringComparison.OrdinalIgnoreCase)) return 300;
        if (text.Contains(query, StringComparison.OrdinalIgnoreCase)) return 200;

        var qi = 0;
        foreach (var ch in text)
        {
            if (qi < query.Length && char.ToLowerInvariant(ch) == char.ToLowerInvariant(query[qi]))
                qi++;
        }
        return qi == query.Length ? 100 : 0;
    }
}
