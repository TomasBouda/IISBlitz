using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml.Linq;
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

    [ObservableProperty]
    private string _logSearchText = string.Empty;

    [ObservableProperty]
    private string _logSearchStatus = string.Empty;

    [ObservableProperty]
    private ObservableCollection<LogSearchResult> _logSearchResults = new();

    [ObservableProperty]
    private bool _hasLogSearchResults;

    [ObservableProperty]
    private string? _logHighlightTerm;

    [ObservableProperty]
    private ObservableCollection<EventLogItem> _eventLogEntries = new();

    [ObservableProperty]
    private string _eventLogFilter = "All";

    [ObservableProperty]
    private string _newPermissionIdentity = string.Empty;

    [ObservableProperty]
    private string _newPermissionRights = "ReadAndExecute";

    [ObservableProperty]
    private string _newPermissionType = "Allow";

    /// <summary>Total working set of the selected site's worker processes, formatted for the overview card.</summary>
    [ObservableProperty]
    private string _workerMemoryText = "—";

    [ObservableProperty]
    private int _runningSiteCount;

    [ObservableProperty]
    private bool _isLoadingEvents;

    [ObservableProperty]
    private EventLogItem? _selectedEvent;

    /// <summary>Result of probing GET {site}/health: null while probing or when no site is selected.</summary>
    [ObservableProperty]
    private bool? _hasHealthEndpoint;

    [ObservableProperty]
    private string _healthEndpointStatus = "—";

    [ObservableProperty]
    private string _healthEndpointDetail = string.Empty;

    /// <summary>True while a site is being loaded; suppresses reactions to property churn during setup.</summary>
    private bool _loadingSite;
    private int _healthProbeVersion;

    partial void OnEventLogFilterChanged(string value) => FilterEventLog(value);

    private SiteInfo? _selectedSite;
    public SiteInfo? SelectedSite
    {
        get => _selectedSite;
        set
        {
            if (_selectedSite != value)
            {
                if (_selectedSite != null)
                    _selectedSite.PropertyChanged -= OnSelectedSitePropertyChanged;

                _selectedSite = value;
                OnPropertyChanged();
                ClearSiteContext();

                if (_selectedSite != null)
                    _selectedSite.PropertyChanged += OnSelectedSitePropertyChanged;

                LoadSiteDetails();
                UpdateStatusText();
                _ = ProbeHealthEndpointAsync();
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
    public ICommand SaveAppSettingsCmd { get; }
    public ICommand SaveWebConfigCmd { get; }
    public ICommand ReloadAppSettingsCmd { get; }
    public ICommand ReloadWebConfigCmd { get; }
    public ICommand RecyclePoolCmd { get; }
    public ICommand ViewLogCmd { get; }
    public ICommand ToggleThemeCmd { get; }
    public ICommand LoadWorkerProcessesCmd { get; }
    public ICommand SearchLogCmd { get; }
    public ICommand SetEnvironmentCmd { get; }
    public ICommand ViewSearchResultCmd { get; }
    public ICommand ClearLogSearchCmd { get; }
    public ICommand LoadEventLogCmd { get; }
    public ICommand FilterEventLogCmd { get; }
    public ICommand LoadPermissionsCmd { get; }
    public ICommand AddPermissionCmd { get; }
    public ICommand RemovePermissionCmd { get; }

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
        SaveAppSettingsCmd = ReactiveCommand.Create(SaveAppSettings);
        SaveWebConfigCmd = ReactiveCommand.Create(SaveWebConfig);
        ReloadAppSettingsCmd = ReactiveCommand.Create(ReloadAppSettings);
        ReloadWebConfigCmd = ReactiveCommand.Create(ReloadWebConfig);
        RecyclePoolCmd = ReactiveCommand.Create(RecycleAppPool);
        ViewLogCmd = ReactiveCommand.Create<string>(ViewLog);
        ToggleThemeCmd = ReactiveCommand.Create(ToggleTheme);
        LoadWorkerProcessesCmd = ReactiveCommand.Create(LoadWorkerProcesses);
        SearchLogCmd = ReactiveCommand.Create(SearchLog);
        SetEnvironmentCmd = ReactiveCommand.Create<string?>(SetEnvironment);
        ViewSearchResultCmd = ReactiveCommand.Create<string>(ViewLogFromSearch);
        ClearLogSearchCmd = ReactiveCommand.Create(ClearLogSearch);
        LoadEventLogCmd = ReactiveCommand.Create(LoadEventLog);
        FilterEventLogCmd = ReactiveCommand.Create<string?>(FilterEventLog);
        LoadPermissionsCmd = ReactiveCommand.Create(LoadPermissions);
        AddPermissionCmd = ReactiveCommand.Create(AddPermission);
        RemovePermissionCmd = ReactiveCommand.Create<string?>(RemovePermission);

        _serverManager = new ServerManager();
        LoadIISSites();
        ApplyFilter();
    }

    /// <summary>Picking a file chip loads that appsettings file into the editor; Apply writes its environment to web.config.</summary>
    private void OnSelectedSitePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(SiteInfo.SelectedAppSettingsFile)) return;
        OnPropertyChanged(nameof(SelectedAppSettingsEnvironment));
        OnPropertyChanged(nameof(CanApplySelectedEnvironment));
        if (!_loadingSite && SelectedSite?.SelectedAppSettingsFile is { Length: > 0 })
            LoadSelectedAppSettingsFile();
    }

    /// <summary>Environment encoded in the selected file name ("appsettings.Production.json" → "Production"); null for the base file.</summary>
    public string? SelectedAppSettingsEnvironment => EnvironmentFromFileName(SelectedSite?.SelectedAppSettingsFile);

    public bool CanApplySelectedEnvironment => SelectedAppSettingsEnvironment != null;

    private static string? EnvironmentFromFileName(string? fileName)
    {
        if (fileName == null || !fileName.StartsWith("appsettings.", StringComparison.OrdinalIgnoreCase)
            || !fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            return null;
        var env = fileName["appsettings.".Length..^".json".Length];
        return env.Length > 0 ? env : null;
    }

    private static string FileNameForEnvironment(string env) => $"appsettings.{env}.json";

    private void LoadSelectedAppSettingsFile()
    {
        if (SelectedSite?.SelectedAppSettingsFile is not { Length: > 0 } settingsFile) return;

        var settingsPath = Path.Combine(SelectedSite.PhysicalPath, settingsFile);
        try
        {
            SelectedSite.AppSettingsContent = File.Exists(settingsPath)
                ? File.ReadAllText(settingsPath)
                : $"// {settingsFile} does not exist yet — Save creates it.\n{{\n}}";
        }
        catch (Exception ex)
        {
            SelectedSite.AppSettingsContent = $"// Error reading {settingsFile}: {ex.Message}";
        }
    }

    /// <summary>
    /// Looks for the conventional GET /health endpoint and, when present, surfaces its status and version
    /// on the overview so a deployed build can be identified at a glance.
    /// </summary>
    private async Task ProbeHealthEndpointAsync()
    {
        var version = ++_healthProbeVersion;
        HasHealthEndpoint = null;
        HealthEndpointStatus = "—";
        HealthEndpointDetail = string.Empty;

        var baseUrl = SelectedSite?.Url;
        if (baseUrl == null)
        {
            HasHealthEndpoint = false;
            HealthEndpointDetail = "no binding";
            return;
        }

        try
        {
            using var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, _, _, _) => true };
            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(4) };
            using var response = await client.GetAsync(baseUrl.TrimEnd('/') + "/health");
            if (version != _healthProbeVersion) return;

            if (!response.IsSuccessStatusCode)
            {
                HasHealthEndpoint = false;
                HealthEndpointDetail = $"HTTP {(int)response.StatusCode}";
                return;
            }

            var body = await response.Content.ReadAsStringAsync();
            if (version != _healthProbeVersion) return;

            HasHealthEndpoint = true;
            try
            {
                using var json = System.Text.Json.JsonDocument.Parse(body);
                var root = json.RootElement;
                HealthEndpointStatus = root.TryGetProperty("status", out var st) ? st.ToString() : "OK";
                var parts = new List<string>();
                if (root.TryGetProperty("name", out var name)) parts.Add(name.ToString());
                if (root.TryGetProperty("version", out var ver)) parts.Add("v" + ver.ToString().TrimStart('v'));
                HealthEndpointDetail = parts.Count > 0 ? string.Join(" · ", parts) : "/health";
            }
            catch (System.Text.Json.JsonException)
            {
                HealthEndpointStatus = body.Trim().Length is > 0 and <= 40 ? body.Trim() : "OK";
                HealthEndpointDetail = "/health (plain text)";
            }
        }
        catch (Exception)
        {
            if (version != _healthProbeVersion) return;
            HasHealthEndpoint = false;
            HealthEndpointDetail = "unreachable";
        }
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
        RunningSiteCount = SiteList.Count(s => s.IsRunning);
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

    private void ClearSiteContext()
    {
        // Clear ViewModel-level state that doesn't belong to SiteInfo
        WorkerMemoryText = "—";
        SelectedEvent = null;
        HasHealthEndpoint = null;
        HealthEndpointStatus = "—";
        HealthEndpointDetail = string.Empty;
        EventLogEntries = new ObservableCollection<EventLogItem>();
        LogSearchText = string.Empty;
        LogSearchStatus = string.Empty;
        LogSearchResults = new ObservableCollection<LogSearchResult>();
        HasLogSearchResults = false;
        LogHighlightTerm = null;
    }

    private void RefreshSites()
    {
        var selectedName = SelectedSite?.Name;
        _serverManager = new ServerManager();
        SiteList.Clear();
        LoadIISSites();
        ApplyFilter();

        // Restore selection by name
        if (selectedName != null)
        {
            SelectedSite = FilteredSiteList.FirstOrDefault(s =>
                s.Name.Equals(selectedName, StringComparison.OrdinalIgnoreCase));
        }
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

    private void ViewLog(string logPath)
    {
        if (SelectedSite == null || string.IsNullOrEmpty(logPath)) return;
        try
        {
            SelectedSite.SelectedLogPath = Path.GetFileName(logPath);
            var lines = File.ReadLines(logPath).TakeLast(500);
            SelectedSite.SelectedLogContent = string.Join(Environment.NewLine, lines);
            ClearLogSearch();
        }
        catch (Exception ex)
        {
            SelectedSite.SelectedLogContent = $"Error reading log: {ex.Message}";
        }
    }

    private void ViewLogFromSearch(string logPath)
    {
        if (SelectedSite == null || string.IsNullOrEmpty(logPath)) return;
        try
        {
            SelectedSite.SelectedLogPath = Path.GetFileName(logPath);
            var lines = File.ReadLines(logPath).TakeLast(500);
            SelectedSite.SelectedLogContent = string.Join(Environment.NewLine, lines);
        }
        catch (Exception ex)
        {
            SelectedSite.SelectedLogContent = $"Error reading log: {ex.Message}";
        }
    }

    private void ClearLogSearch()
    {
        LogSearchText = string.Empty;
        LogSearchStatus = string.Empty;
        LogSearchResults = new ObservableCollection<LogSearchResult>();
        HasLogSearchResults = false;
        LogHighlightTerm = null;
    }

    private void SearchLog()
    {
        if (SelectedSite?.Logs == null || string.IsNullOrWhiteSpace(LogSearchText))
        {
            ClearLogSearch();
            return;
        }

        var results = new ObservableCollection<LogSearchResult>();
        var totalMatches = 0;

        foreach (var logPath in SelectedSite.Logs)
        {
            try
            {
                var content = File.ReadAllText(logPath);
                var count = 0;
                var idx = 0;
                while ((idx = content.IndexOf(LogSearchText, idx, StringComparison.OrdinalIgnoreCase)) >= 0)
                {
                    count++;
                    idx += LogSearchText.Length;
                }
                if (count > 0)
                {
                    results.Add(new LogSearchResult(Path.GetFileName(logPath), logPath, count));
                    totalMatches += count;
                }
            }
            catch { }
        }

        LogSearchResults = results;
        HasLogSearchResults = results.Count > 0;
        LogSearchStatus = totalMatches > 0
            ? $"{totalMatches} matches in {results.Count} files"
            : "No matches";
        LogHighlightTerm = totalMatches > 0 ? LogSearchText : null;

        // Auto-open first matching file
        if (results.Count > 0)
        {
            ViewLogFromSearch(results[0].FullPath);
        }
    }

    /// <summary>ASPNETCORE_ENVIRONMENT from web.config, or null when the site does not set one (ASP.NET Core then runs as Production).</summary>
    private static string? ReadEnvironmentFromWebConfig(string physicalPath)
    {
        var webConfigPath = Path.Combine(physicalPath, "web.config");
        if (!File.Exists(webConfigPath)) return null;

        try
        {
            var doc = XDocument.Load(webConfigPath);
            var envVar = doc.Descendants("environmentVariable")
                .FirstOrDefault(e => (string?)e.Attribute("name") == "ASPNETCORE_ENVIRONMENT");
            var value = (string?)envVar?.Attribute("value");
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
        catch
        {
            return null;
        }
    }

    private void SetEnvironment(string? env)
    {
        if (SelectedSite == null || string.IsNullOrEmpty(env)) return;

        var webConfigPath = Path.Combine(SelectedSite.PhysicalPath, "web.config");

        try
        {
            if (!File.Exists(webConfigPath)) return;

            var doc = XDocument.Load(webConfigPath);

            var aspNetCore = doc.Descendants("aspNetCore").FirstOrDefault();
            if (aspNetCore == null) return;

            var envVars = aspNetCore.Element("environmentVariables");
            if (envVars == null)
            {
                envVars = new XElement("environmentVariables");
                aspNetCore.Add(envVars);
            }

            var envVarElement = envVars.Elements("environmentVariable")
                .FirstOrDefault(e => (string?)e.Attribute("name") == "ASPNETCORE_ENVIRONMENT");

            if (envVarElement != null)
            {
                envVarElement.SetAttributeValue("value", env);
            }
            else
            {
                envVars.Add(new XElement("environmentVariable",
                    new XAttribute("name", "ASPNETCORE_ENVIRONMENT"),
                    new XAttribute("value", env)));
            }

            doc.Save(webConfigPath);

            SelectedSite.CurrentEnvironment = env;
            SelectedSite.ConfiguredEnvironmentText = env;

            // Reload web.config content in editor
            ReloadWebConfig();
            SelectedSite.SelectedAppSettingsFile = FileNameForEnvironment(env);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to set environment: {ex.Message}");
        }
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

    private void ToggleTheme()
    {
        if (Avalonia.Application.Current == null) return;
        var next = Avalonia.Application.Current.ActualThemeVariant == Avalonia.Styling.ThemeVariant.Dark
            ? Avalonia.Styling.ThemeVariant.Light
            : Avalonia.Styling.ThemeVariant.Dark;
        Avalonia.Application.Current.RequestedThemeVariant = next;

        var settings = Services.UserSettings.Load();
        settings.Theme = next == Avalonia.Styling.ThemeVariant.Dark ? "Dark" : "Light";
        settings.Save();
    }

    private static readonly string[] EventSourceHints =
        { "IIS", "W3SVC", "WAS", "ASP.NET", "ASP.NET Core", ".NET Runtime", "IIS-W3SVC-WP", "IIS AspNetCore Module" };

    private async void LoadEventLog()
    {
        if (IsLoadingEvents) return;
        IsLoadingEvents = true;
        SelectedEvent = null;
        var filter = EventLogFilter;

        try
        {
            var items = await Task.Run(() => ReadRecentEvents(TimeSpan.FromHours(24)));

            if (filter != "All")
                items = items.Where(e => e.Level.Equals(filter, StringComparison.OrdinalIgnoreCase)).ToList();

            EventLogEntries = new ObservableCollection<EventLogItem>(items);
        }
        catch (Exception ex)
        {
            EventLogEntries = new ObservableCollection<EventLogItem>(
                [new EventLogItem(DateTime.Now, "Error", "IISBlitz", $"Failed to read event log: {ex.Message}")]);
        }
        finally
        {
            IsLoadingEvents = false;
        }
    }

    /// <summary>
    /// Reads IIS / ASP.NET related events from the Application and System logs.
    /// The time window is pushed into the XPath query so the reader only touches recent records
    /// instead of enumerating the whole log through the legacy EventLog API.
    /// </summary>
    private static List<EventLogItem> ReadRecentEvents(TimeSpan window)
    {
        var result = new List<EventLogItem>();
        var query = $"*[System[TimeCreated[timediff(@SystemTime) <= {(long)window.TotalMilliseconds}]]]";

        foreach (var logName in new[] { "Application", "System" })
        {
            try
            {
                var eventQuery = new System.Diagnostics.Eventing.Reader.EventLogQuery(logName,
                    System.Diagnostics.Eventing.Reader.PathType.LogName, query) { ReverseDirection = true };
                using var reader = new System.Diagnostics.Eventing.Reader.EventLogReader(eventQuery);

                var taken = 0;
                while (taken < 300 && reader.ReadEvent() is { } record)
                {
                    using (record)
                    {
                        var source = record.ProviderName ?? string.Empty;
                        if (!EventSourceHints.Any(h => source.Contains(h, StringComparison.OrdinalIgnoreCase)))
                            continue;

                        var level = record.Level switch
                        {
                            1 or 2 => "Error",
                            3 => "Warning",
                            _ => "Info",
                        };

                        string message;
                        try { message = record.FormatDescription() ?? string.Empty; }
                        catch { message = string.Join(" ", record.Properties.Select(p => p.Value?.ToString())); }

                        result.Add(new EventLogItem(record.TimeCreated ?? DateTime.Now, level, source, message.Trim()));
                        taken++;
                    }
                }
            }
            catch (Exception ex)
            {
                result.Add(new EventLogItem(DateTime.Now, "Warning", "IISBlitz", $"Could not read the {logName} log: {ex.Message}"));
            }
        }

        return result.OrderByDescending(e => e.TimeGenerated).ToList();
    }

    private void FilterEventLog(string? filter)
    {
        EventLogFilter = filter ?? "All";
        LoadEventLog();
    }

    private void LoadWorkerProcesses()
    {
        if (SelectedSite == null) return;
        try
        {
            _serverManager = new ServerManager();
            var appPool = _serverManager.ApplicationPools[SelectedSite.AppPool];
            var workers = new ObservableCollection<WorkerProcessInfo>();

            foreach (var wp in appPool.WorkerProcesses)
            {
                long memKb = 0;
                try
                {
                    var proc = System.Diagnostics.Process.GetProcessById(wp.ProcessId);
                    memKb = proc.WorkingSet64 / 1024;
                }
                catch { }

                workers.Add(new WorkerProcessInfo(
                    wp.ProcessId,
                    SelectedSite.AppPool,
                    wp.State.ToString(),
                    memKb));
            }

            SelectedSite.WorkerProcesses = workers;
            var totalKb = workers.Sum(w => w.MemoryKb);
            WorkerMemoryText = workers.Count == 0 ? "—" : $"{totalKb / 1024} MB";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load worker processes: {ex.Message}");
        }
    }

    private void StartWebsite()
    {
        if (SelectedSite == null) return;
        try
        {
            var site = _serverManager.Sites[SelectedSite.Name];
            site.Start();
            RefreshSites();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start site: {ex.Message}");
        }
    }

    private void StopWebsite()
    {
        if (SelectedSite == null) return;
        try
        {
            var site = _serverManager.Sites[SelectedSite.Name];
            site.Stop();
            RefreshSites();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to stop site: {ex.Message}");
        }
    }

    private void StartAppPool()
    {
        if (SelectedSite == null) return;
        try
        {
            _serverManager = new ServerManager();
            var appPool = _serverManager.ApplicationPools[SelectedSite.AppPool];
            if (appPool.State == ObjectState.Stopped)
                appPool.Start();
            RefreshSites();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start app pool: {ex.Message}");
        }
    }

    private void StopAppPool()
    {
        if (SelectedSite == null) return;
        try
        {
            _serverManager = new ServerManager();
            var appPool = _serverManager.ApplicationPools[SelectedSite.AppPool];
            if (appPool.State == ObjectState.Started)
                appPool.Stop();
            RefreshSites();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to stop app pool: {ex.Message}");
        }
    }

    private void RecycleAppPool()
    {
        if (SelectedSite == null) return;
        try
        {
            _serverManager = new ServerManager();
            var appPool = _serverManager.ApplicationPools[SelectedSite.AppPool];
            appPool.Recycle();
            RefreshSites();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to recycle app pool: {ex.Message}");
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

    private void SaveAppSettings()
    {
        if (SelectedSite?.AppSettingsContent == null) return;
        try
        {
            var fileName = SelectedSite.SelectedAppSettingsFile ?? "appsettings.json";
            var path = Path.Combine(SelectedSite.PhysicalPath, fileName);
            File.WriteAllText(path, SelectedSite.AppSettingsContent);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save appsettings: {ex.Message}");
        }
    }

    private void SaveWebConfig()
    {
        if (SelectedSite?.WebConfigContent == null) return;
        try
        {
            var path = Path.Combine(SelectedSite.PhysicalPath, "web.config");
            File.WriteAllText(path, SelectedSite.WebConfigContent);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save web.config: {ex.Message}");
        }
    }

    private void ReloadAppSettings()
    {
        if (SelectedSite == null) return;
        try
        {
            var fileName = SelectedSite.SelectedAppSettingsFile ?? "appsettings.json";
            var path = Path.Combine(SelectedSite.PhysicalPath, fileName);
            SelectedSite.AppSettingsContent = File.Exists(path)
                ? File.ReadAllText(path)
                : $"// File not found: {fileName}";
        }
        catch (Exception ex)
        {
            SelectedSite.AppSettingsContent = $"// Error reading: {ex.Message}";
        }
    }

    private void ReloadWebConfig()
    {
        if (SelectedSite == null) return;
        try
        {
            var path = Path.Combine(SelectedSite.PhysicalPath, "web.config");
            SelectedSite.WebConfigContent = File.Exists(path)
                ? File.ReadAllText(path)
                : "<!-- File not found: web.config -->";
        }
        catch (Exception ex)
        {
            SelectedSite.WebConfigContent = $"<!-- Error reading web.config: {ex.Message} -->";
        }
    }

    private void LoadSiteDetails()
    {
        if (SelectedSite == null) return;
        _loadingSite = true;
        try
        {
            LoadSiteDetailsCore();
        }
        finally
        {
            _loadingSite = false;
        }
    }

    private void LoadSiteDetailsCore()
    {
        if (SelectedSite == null) return;

        // Read environment from web.config; when unset ASP.NET Core behaves as Production.
        var configuredEnv = ReadEnvironmentFromWebConfig(SelectedSite.PhysicalPath);
        SelectedSite.ConfiguredEnvironmentText = configuredEnv ?? "not set → Production";
        var currentEnv = configuredEnv ?? "Production";

        // Build available environments from appsettings files + defaults.
        // The list is assigned before the current value: replacing the list would otherwise reset the selection.
        var envs = new System.Collections.Generic.List<string> { "Development", "Production" };
        try
        {
            var detected = Directory.GetFiles(SelectedSite.PhysicalPath, "appsettings.*.json")
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .Select(f => f!.Replace("appsettings.", ""))
                .Where(e => !string.IsNullOrEmpty(e));
            foreach (var e in detected)
            {
                if (!envs.Contains(e, StringComparer.OrdinalIgnoreCase))
                    envs.Add(e);
            }
        }
        catch { }
        if (!envs.Contains(currentEnv, StringComparer.OrdinalIgnoreCase))
            envs.Add(currentEnv);
        SelectedSite.AvailableEnvironments = new ObservableCollection<string>(
            envs.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(e => e));
        SelectedSite.CurrentEnvironment = currentEnv;

        // File chips: the base file, Development and Production always, plus whatever else exists on disk.
        // The list is assigned before the selection: replacing it afterwards would reset the selection to null.
        var files = new List<string> { "appsettings.json", FileNameForEnvironment("Development"), FileNameForEnvironment("Production") };
        foreach (var env in envs)
        {
            var name = FileNameForEnvironment(env);
            if (!files.Contains(name, StringComparer.OrdinalIgnoreCase))
                files.Add(name);
        }
        SelectedSite.AppSettingsFiles = new ObservableCollection<string>(files);

        // Open the file of the environment web.config currently points to.
        SelectedSite.SelectedAppSettingsFile = FileNameForEnvironment(currentEnv);
        LoadSelectedAppSettingsFile();

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

        LoadCertificates();
        LoadWorkerProcesses();
        LoadPermissions();
    }

    private void LoadCertificates()
    {
        if (SelectedSite == null) return;
        try
        {
            var certs = new ObservableCollection<CertificateInfo>();
            var site = _serverManager.Sites[SelectedSite.Name];

            foreach (var binding in site.Bindings)
            {
                if (binding.Protocol.Equals("https", StringComparison.OrdinalIgnoreCase)
                    && binding.CertificateHash != null
                    && binding.CertificateHash.Length > 0)
                {
                    var storeName = binding.CertificateStoreName ?? "My";
                    using var store = new System.Security.Cryptography.X509Certificates.X509Store(
                        storeName, System.Security.Cryptography.X509Certificates.StoreLocation.LocalMachine);
                    store.Open(System.Security.Cryptography.X509Certificates.OpenFlags.ReadOnly);

                    var thumbprint = BitConverter.ToString(binding.CertificateHash).Replace("-", "");
                    var found = store.Certificates.Find(
                        System.Security.Cryptography.X509Certificates.X509FindType.FindByThumbprint,
                        thumbprint, false);

                    if (found.Count > 0)
                    {
                        var cert = found[0];
                        var parts = binding.BindingInformation.Split(':');
                        var port = parts.Length >= 2 ? parts[1] : "443";
                        certs.Add(new CertificateInfo(
                            cert.Subject,
                            cert.Issuer,
                            cert.NotAfter,
                            thumbprint,
                            port));
                    }
                }
            }

            SelectedSite.Certificates = certs;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load certificates: {ex.Message}");
        }
    }

    private void LoadPermissions()
    {
        if (SelectedSite == null) return;
        try
        {
            var dirInfo = new DirectoryInfo(SelectedSite.PhysicalPath);
            var acl = dirInfo.GetAccessControl();
            var rules = acl.GetAccessRules(true, true, typeof(NTAccount));
            var permissions = new ObservableCollection<FolderPermissionEntry>();

            foreach (FileSystemAccessRule rule in rules)
            {
                permissions.Add(new FolderPermissionEntry(
                    rule.IdentityReference.Value,
                    rule.FileSystemRights.ToString(),
                    rule.AccessControlType.ToString(),
                    rule.IsInherited));
            }

            SelectedSite.Permissions = permissions;
        }
        catch (Exception ex)
        {
            SelectedSite.Permissions = new ObservableCollection<FolderPermissionEntry>(
                [new FolderPermissionEntry($"Error: {ex.Message}", "", "", false)]);
        }
    }

    private void AddPermission()
    {
        if (SelectedSite == null || string.IsNullOrWhiteSpace(NewPermissionIdentity)) return;
        try
        {
            var dirInfo = new DirectoryInfo(SelectedSite.PhysicalPath);
            var acl = dirInfo.GetAccessControl();

            var rights = NewPermissionRights switch
            {
                "FullControl" => FileSystemRights.FullControl,
                "Modify" => FileSystemRights.Modify,
                "ReadAndExecute" => FileSystemRights.ReadAndExecute,
                "Read" => FileSystemRights.Read,
                "Write" => FileSystemRights.Write,
                "ListDirectory" => FileSystemRights.ListDirectory,
                _ => FileSystemRights.ReadAndExecute
            };

            var accessType = NewPermissionType == "Deny"
                ? AccessControlType.Deny
                : AccessControlType.Allow;

            var rule = new FileSystemAccessRule(
                new NTAccount(NewPermissionIdentity),
                rights,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                accessType);

            acl.AddAccessRule(rule);
            dirInfo.SetAccessControl(acl);

            NewPermissionIdentity = string.Empty;
            LoadPermissions();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to add permission: {ex.Message}");
        }
    }

    private void RemovePermission(string? identity)
    {
        if (SelectedSite == null || string.IsNullOrEmpty(identity)) return;
        try
        {
            var dirInfo = new DirectoryInfo(SelectedSite.PhysicalPath);
            var acl = dirInfo.GetAccessControl();
            var rules = acl.GetAccessRules(true, false, typeof(NTAccount));

            foreach (FileSystemAccessRule rule in rules)
            {
                if (rule.IdentityReference.Value.Equals(identity, StringComparison.OrdinalIgnoreCase)
                    && !rule.IsInherited)
                {
                    acl.RemoveAccessRule(rule);
                }
            }

            dirInfo.SetAccessControl(acl);
            LoadPermissions();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to remove permission: {ex.Message}");
        }
    }
}
