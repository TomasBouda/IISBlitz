using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Web.Administration;
using ReactiveUI;
using TomLabs.IISBlitz.App.Models;

namespace TomLabs.IISBlitz.App.ViewModels;

public partial class SiteViewModel : ObservableObject
{
    private ServerManager _serverManager;

    [ObservableProperty]
    private ObservableCollection<SiteInfo> _siteList = new();

    [ObservableProperty]
    private ObservableCollection<SiteInfo> _filteredSiteList = new();

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilter();
            }
        }
    }

    [ObservableProperty]
    private int _totalSiteCount;

    [ObservableProperty]
    private string _statusText = string.Empty;

    private SiteInfo? _selectedSite;
    public SiteInfo? SelectedSite
    {
        get => _selectedSite;
        set
        {
            if (_selectedSite != value)
            {
                _selectedSite = value;
                OnPropertyChanged();
                LoadSiteDetails();
                UpdateStatusText();
            }
        }
    }

    public ICommand StartWebCmd { get; }
    public ICommand StopWebCmd { get; }
    public ICommand StartPoolCmd { get; }
    public ICommand StopPoolCmd { get; }
    public ICommand OpenAppSettingsCmd { get; }
    public ICommand OpenWebConfigCmd { get; }
    public ICommand OpenWebFolderCmd { get; }
    public ICommand OpenWebLogCmd { get; }
    public ICommand RefreshSitesCmd { get; }
    public ICommand BrowseSiteCmd { get; }
    public ICommand CopyPathCmd { get; }
    public ICommand RestartSiteCmd { get; }
    public ICommand RefreshLogsCmd { get; }

    public SiteViewModel()
    {
        StartWebCmd = ReactiveCommand.Create(StartWebsite);
        StopWebCmd = ReactiveCommand.Create(StopWebsite);
        StartPoolCmd = ReactiveCommand.Create(StartAppPool);
        StopPoolCmd = ReactiveCommand.Create(StopAppPool);

        OpenAppSettingsCmd = ReactiveCommand.Create(OpenSiteAppSettings);
        OpenWebConfigCmd = ReactiveCommand.Create(OpenSiteWebConfig);
        OpenWebFolderCmd = ReactiveCommand.Create(OpenWebFolder);
        OpenWebLogCmd = ReactiveCommand.Create<string>(OpenLog);

        RefreshSitesCmd = ReactiveCommand.Create(RefreshSites);
        BrowseSiteCmd = ReactiveCommand.Create(BrowseSite);
        CopyPathCmd = ReactiveCommand.Create(CopyPath);
        RestartSiteCmd = ReactiveCommand.Create(RestartSite);
        RefreshLogsCmd = ReactiveCommand.Create(RefreshLogs);

        _serverManager = new ServerManager();
        LoadIISSites();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? SiteList
            : new ObservableCollection<SiteInfo>(
                SiteList.Where(s =>
                    s.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    s.AppPool.Contains(SearchText, StringComparison.OrdinalIgnoreCase)));

        FilteredSiteList = filtered;
        TotalSiteCount = SiteList.Count;
        UpdateStatusText();

        // Preserve selection if still visible
        if (SelectedSite != null && !FilteredSiteList.Contains(SelectedSite))
        {
            SelectedSite = FilteredSiteList.FirstOrDefault();
        }
    }

    private void UpdateStatusText()
    {
        StatusText = SelectedSite != null
            ? $"Selected: {SelectedSite.Name} — {(SelectedSite.IsRunning ? "Running" : "Stopped")}"
            : "No site selected";
    }

    private void RefreshSites()
    {
        _serverManager = new ServerManager();
        SiteList.Clear();
        LoadIISSites();
        ApplyFilter();
    }

    private void BrowseSite()
    {
        if (SelectedSite?.Url is { } url)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true,
            });
        }
    }

    private async void CopyPath()
    {
        if (SelectedSite == null) return;

        try
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                && desktop.MainWindow is { } window)
            {
                var clipboard = TopLevel.GetTopLevel(window)?.Clipboard;
                if (clipboard != null)
                {
                    await clipboard.SetTextAsync(SelectedSite.PhysicalPath);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to copy path: {ex.Message}");
        }
    }

    private void RestartSite()
    {
        if (SelectedSite == null) return;

        try
        {
            var site = _serverManager.Sites[SelectedSite.Name];
            if (site.State == ObjectState.Started)
                site.Stop();

            site.Start();
            RefreshSites();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to restart site: {ex.Message}");
        }
    }

    private void RefreshLogs()
    {
        if (SelectedSite == null) return;

        var logsDir = Path.Combine(SelectedSite.PhysicalPath, "logs");
        SelectedSite.Logs = Directory.Exists(logsDir)
            ? new ObservableCollection<string>(Directory.GetFiles(logsDir, "*.log", SearchOption.AllDirectories))
            : null;
    }

    private void OpenLog(string log)
    {
        if (SelectedSite != null)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = log,
                UseShellExecute = true,
                Verb = "open"
            });
        }
    }

    private void StartWebsite()
    {
        if (SelectedSite != null)
        {
            var site = _serverManager.Sites[SelectedSite.Name];
            site.Start();
            RefreshSites();
        }
    }

    private void StopWebsite()
    {
        if (SelectedSite != null)
        {
            var site = _serverManager.Sites[SelectedSite.Name];
            site.Stop();
            RefreshSites();
        }
    }

    private void StartAppPool()
    {
        if (SelectedSite != null)
        {
            var appPool = _serverManager.ApplicationPools[SelectedSite.AppPool];
            appPool.Start();
            RefreshSites();
        }
    }

    private void StopAppPool()
    {
        if (SelectedSite != null)
        {
            var appPool = _serverManager.ApplicationPools[SelectedSite.AppPool];
            appPool.Stop();
            RefreshSites();
        }
    }

    private void OpenWebFolder()
    {
        if (SelectedSite != null)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = SelectedSite.PhysicalPath,
                UseShellExecute = true,
                Verb = "open"
            });
        }
    }

    private void OpenSiteAppSettings()
    {
        if (SelectedSite != null)
        {
            OpenFileInDefaultEditor(Path.Combine(SelectedSite.PhysicalPath, "appsettings.json"));
        }
    }

    private void OpenSiteWebConfig()
    {
        if (SelectedSite != null)
        {
            OpenFileInDefaultEditor(Path.Combine(SelectedSite.PhysicalPath, "web.config"));
        }
    }

    private void OpenFileInDefaultEditor(string filePath)
    {
        if (File.Exists(filePath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true,
            });
        }
    }

    private void LoadIISSites()
    {
        foreach (var site in _serverManager.Sites)
        {
            try
            {
                var existingSite = SiteList.FirstOrDefault(s => s.Name == site.Name);
                var appPool = _serverManager.ApplicationPools[site.Applications[0].ApplicationPoolName];
                var sitePath = site.Applications[0].VirtualDirectories[0].PhysicalPath;
                sitePath = Environment.ExpandEnvironmentVariables(sitePath);
                var logsDir = Path.Combine(sitePath, "logs");

                var bindings = new ObservableCollection<BindingInfo>(
                    site.Bindings.Select(b =>
                    {
                        var parts = b.BindingInformation.Split(':');
                        var port = parts.Length >= 2 && int.TryParse(parts[1], out var p) ? p : 0;
                        var host = parts.Length >= 3 ? parts[2] : string.Empty;
                        return new BindingInfo(b.Protocol, host, port, b.BindingInformation);
                    }));

                if (existingSite == null)
                {
                    SiteList.Add(new SiteInfo
                    {
                        Name = site.Name,
                        IsRunning = site.State == ObjectState.Started,
                        IsPoolRunning = appPool.State == ObjectState.Started,
                        AppPool = site.Applications[0].ApplicationPoolName,
                        PhysicalPath = sitePath,
                        Bindings = bindings,
                        Logs = Directory.Exists(logsDir)
                            ? new ObservableCollection<string>(Directory.GetFiles(logsDir, "*.log", SearchOption.AllDirectories))
                            : null
                    });
                }
                else
                {
                    existingSite.IsRunning = site.State == ObjectState.Started;
                    existingSite.IsPoolRunning = appPool.State == ObjectState.Started;
                    existingSite.Bindings = bindings;
                    existingSite.Logs = Directory.Exists(logsDir)
                        ? new ObservableCollection<string>(Directory.GetFiles(logsDir, "*.log", SearchOption.AllDirectories))
                        : null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading site {site.Name}: {ex.Message}");
            }
        }
    }

    private void LoadSiteDetails()
    {
        if (SelectedSite == null) return;

        try
        {
            var appSettingsPath = Path.Combine(SelectedSite.PhysicalPath, "appsettings.json");
            SelectedSite.AppSettingsContent = File.Exists(appSettingsPath)
                ? File.ReadAllText(appSettingsPath)
                : "// File not found: appsettings.json";
        }
        catch (Exception ex)
        {
            SelectedSite.AppSettingsContent = $"// Error reading appsettings.json: {ex.Message}";
        }

        try
        {
            var webConfigPath = Path.Combine(SelectedSite.PhysicalPath, "web.config");
            SelectedSite.WebConfigContent = File.Exists(webConfigPath)
                ? File.ReadAllText(webConfigPath)
                : "<!-- File not found: web.config -->";
        }
        catch (Exception ex)
        {
            SelectedSite.WebConfigContent = $"<!-- Error reading web.config: {ex.Message} -->";
        }
    }
}