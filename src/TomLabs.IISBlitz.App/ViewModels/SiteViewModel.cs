using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
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
    private string _healthCheckResult = string.Empty;

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
    private ObservableCollection<HealthCheckEntry> _healthCheckHistory = new();

    [ObservableProperty]
    private List<double> _responseTimeValues = new();

    [ObservableProperty]
    private string _eventLogFilter = "All";

    [ObservableProperty]
    private SiteResponseInfo? _siteResponse;

    [ObservableProperty]
    private bool _isLoadingResponse;

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
                ClearSiteContext();
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
    public ICommand SaveAppSettingsCmd { get; }
    public ICommand SaveWebConfigCmd { get; }
    public ICommand ReloadAppSettingsCmd { get; }
    public ICommand ReloadWebConfigCmd { get; }
    public ICommand RecyclePoolCmd { get; }
    public ICommand HealthCheckCmd { get; }
    public ICommand ViewLogCmd { get; }
    public ICommand ToggleThemeCmd { get; }
    public ICommand LoadWorkerProcessesCmd { get; }
    public ICommand SearchLogCmd { get; }
    public ICommand SetEnvironmentCmd { get; }
    public ICommand ViewSearchResultCmd { get; }
    public ICommand ClearLogSearchCmd { get; }
    public ICommand LoadEventLogCmd { get; }
    public ICommand FilterEventLogCmd { get; }
    public ICommand RunHealthCheckSeriesCmd { get; }
    public ICommand FetchSiteResponseCmd { get; }

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
        HealthCheckCmd = ReactiveCommand.Create(HealthCheck);
        ViewLogCmd = ReactiveCommand.Create<string>(ViewLog);
        ToggleThemeCmd = ReactiveCommand.Create(ToggleTheme);
        LoadWorkerProcessesCmd = ReactiveCommand.Create(LoadWorkerProcesses);
        SearchLogCmd = ReactiveCommand.Create(SearchLog);
        SetEnvironmentCmd = ReactiveCommand.Create<string?>(SetEnvironment);
        ViewSearchResultCmd = ReactiveCommand.Create<string>(ViewLogFromSearch);
        ClearLogSearchCmd = ReactiveCommand.Create(ClearLogSearch);
        LoadEventLogCmd = ReactiveCommand.Create(LoadEventLog);
        FilterEventLogCmd = ReactiveCommand.Create<string?>(FilterEventLog);
        RunHealthCheckSeriesCmd = ReactiveCommand.Create(RunHealthCheckSeries);
        FetchSiteResponseCmd = ReactiveCommand.Create(FetchSiteResponse);

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

    private void ClearSiteContext()
    {
        // Clear ViewModel-level state that doesn't belong to SiteInfo
        HealthCheckResult = string.Empty;
        HealthCheckHistory = new ObservableCollection<HealthCheckEntry>();
        ResponseTimeValues = new List<double>();
        EventLogEntries = new ObservableCollection<EventLogItem>();
        SiteResponse = null;
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

    private string ReadEnvironmentFromWebConfig(string physicalPath)
    {
        var webConfigPath = Path.Combine(physicalPath, "web.config");
        if (!File.Exists(webConfigPath)) return "Production";

        try
        {
            var doc = XDocument.Load(webConfigPath);
            var envVar = doc.Descendants("environmentVariable")
                .FirstOrDefault(e => (string?)e.Attribute("name") == "ASPNETCORE_ENVIRONMENT");
            return (string?)envVar?.Attribute("value") ?? "Production";
        }
        catch
        {
            return "Production";
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

            // Reload web.config content in editor
            ReloadWebConfig();

            // Load corresponding appsettings file
            var settingsFile = env.Equals("Production", StringComparison.OrdinalIgnoreCase)
                ? "appsettings.json"
                : $"appsettings.{env}.json";

            SelectedSite.SelectedAppSettingsFile = settingsFile;
            var settingsPath = Path.Combine(SelectedSite.PhysicalPath, settingsFile);
            SelectedSite.AppSettingsContent = File.Exists(settingsPath)
                ? File.ReadAllText(settingsPath)
                : $"// File not found: {settingsFile}";
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
        var current = Avalonia.Application.Current.RequestedThemeVariant;
        Avalonia.Application.Current.RequestedThemeVariant =
            current == Avalonia.Styling.ThemeVariant.Dark
                ? Avalonia.Styling.ThemeVariant.Light
                : Avalonia.Styling.ThemeVariant.Dark;
    }

    private void LoadEventLog()
    {
        try
        {
            var entries = new ObservableCollection<EventLogItem>();
            var sources = new[] { "IIS", "W3SVC", "WAS", "ASP.NET", "ASP.NET Core", ".NET Runtime", "IIS-W3SVC-WP" };

            using var eventLog = new System.Diagnostics.EventLog("Application");
            var recent = eventLog.Entries.Cast<System.Diagnostics.EventLogEntry>()
                .Where(e => e.TimeGenerated > DateTime.Now.AddHours(-24))
                .Where(e => sources.Any(s => e.Source.Contains(s, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(e => e.TimeGenerated)
                .Take(100);

            foreach (var entry in recent)
            {
                var level = entry.EntryType switch
                {
                    System.Diagnostics.EventLogEntryType.Error => "Error",
                    System.Diagnostics.EventLogEntryType.Warning => "Warning",
                    System.Diagnostics.EventLogEntryType.Information => "Info",
                    _ => entry.EntryType.ToString()
                };
                entries.Add(new EventLogItem(entry.TimeGenerated, level, entry.Source, entry.Message));
            }

            // Also check System log for W3SVC
            using var systemLog = new System.Diagnostics.EventLog("System");
            var systemRecent = systemLog.Entries.Cast<System.Diagnostics.EventLogEntry>()
                .Where(e => e.TimeGenerated > DateTime.Now.AddHours(-24))
                .Where(e => e.Source.Contains("W3SVC", StringComparison.OrdinalIgnoreCase)
                          || e.Source.Contains("WAS", StringComparison.OrdinalIgnoreCase)
                          || e.Source.Contains("IIS", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(e => e.TimeGenerated)
                .Take(50);

            foreach (var entry in systemRecent)
            {
                var level = entry.EntryType switch
                {
                    System.Diagnostics.EventLogEntryType.Error => "Error",
                    System.Diagnostics.EventLogEntryType.Warning => "Warning",
                    System.Diagnostics.EventLogEntryType.Information => "Info",
                    _ => entry.EntryType.ToString()
                };
                entries.Add(new EventLogItem(entry.TimeGenerated, level, entry.Source, entry.Message));
            }

            EventLogEntries = new ObservableCollection<EventLogItem>(
                entries.OrderByDescending(e => e.TimeGenerated));
        }
        catch (Exception ex)
        {
            EventLogEntries = new ObservableCollection<EventLogItem>(
                [new EventLogItem(DateTime.Now, "Error", "IISBlitz", $"Failed to read event log: {ex.Message}")]);
        }
    }

    private void FilterEventLog(string? filter)
    {
        EventLogFilter = filter ?? "All";
        LoadEventLog();

        if (filter != null && filter != "All")
        {
            EventLogEntries = new ObservableCollection<EventLogItem>(
                EventLogEntries.Where(e => e.Level.Equals(filter, StringComparison.OrdinalIgnoreCase)));
        }
    }

    private async void RunHealthCheckSeries()
    {
        if (SelectedSite?.Url == null)
        {
            HealthCheckResult = "No URL available";
            return;
        }

        HealthCheckResult = "Running series (5 pings)...";
        var history = new List<HealthCheckEntry>();

        try
        {
            using var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };
            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };

            for (int i = 0; i < 5; i++)
            {
                try
                {
                    var sw = Stopwatch.StartNew();
                    var response = await client.GetAsync(SelectedSite.Url);
                    sw.Stop();
                    history.Add(new HealthCheckEntry(DateTime.Now, (int)response.StatusCode, sw.ElapsedMilliseconds));
                }
                catch (TaskCanceledException)
                {
                    history.Add(new HealthCheckEntry(DateTime.Now, 0, 10000));
                }
                catch
                {
                    history.Add(new HealthCheckEntry(DateTime.Now, -1, 0));
                }

                if (i < 4)
                    await Task.Delay(1000);
            }

            HealthCheckHistory = new ObservableCollection<HealthCheckEntry>(
                HealthCheckHistory.Concat(history).TakeLast(20));

            ResponseTimeValues = HealthCheckHistory
                .Select(h => (double)h.ResponseTimeMs)
                .ToList();

            var avg = history.Average(h => h.ResponseTimeMs);
            var last = history[^1];
            HealthCheckResult = $"{last.StatusCode} — avg: {avg:F0}ms, last: {last.ResponseTimeMs}ms ({HealthCheckHistory.Count} total)";
        }
        catch (Exception ex)
        {
            HealthCheckResult = $"Error: {ex.Message}";
        }
    }

    private async void FetchSiteResponse()
    {
        if (SelectedSite?.Url == null)
        {
            SiteResponse = null;
            return;
        }

        IsLoadingResponse = true;
        try
        {
            using var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
                AllowAutoRedirect = true
            };
            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
            client.DefaultRequestHeaders.Add("User-Agent", "IISBlitz/0.3");

            var sw = Stopwatch.StartNew();
            var response = await client.GetAsync(SelectedSite.Url);
            var body = await response.Content.ReadAsStringAsync();
            sw.Stop();

            var headers = new List<HttpHeaderItem>();
            foreach (var h in response.Headers)
                headers.Add(new HttpHeaderItem(h.Key, string.Join("; ", h.Value)));
            foreach (var h in response.Content.Headers)
                headers.Add(new HttpHeaderItem(h.Key, string.Join("; ", h.Value)));

            // Parse meta tags from HTML
            var title = ExtractBetween(body, "<title>", "</title>") ?? "";
            var metaDesc = ExtractMetaContent(body, "description") ?? "";
            var metaGen = ExtractMetaContent(body, "generator") ?? "";

            var server = response.Headers.TryGetValues("Server", out var sv)
                ? string.Join(", ", sv) : "";
            var poweredBy = response.Headers.TryGetValues("X-Powered-By", out var xp)
                ? string.Join(", ", xp) : "";

            SiteResponse = new SiteResponseInfo
            {
                StatusCode = (int)response.StatusCode,
                StatusDescription = response.StatusCode.ToString(),
                ResponseTimeMs = sw.ElapsedMilliseconds,
                ContentLength = body.Length,
                ContentType = response.Content.Headers.ContentType?.ToString() ?? "",
                Server = server,
                PoweredBy = poweredBy,
                PageTitle = title,
                MetaDescription = metaDesc,
                MetaGenerator = metaGen,
                Headers = headers,
                RawHtml = body.Length > 50000 ? body[..50000] + "\n... (truncated)" : body
            };
        }
        catch (Exception ex)
        {
            SiteResponse = new SiteResponseInfo
            {
                StatusCode = -1,
                StatusDescription = $"Error: {ex.Message}",
                RawHtml = ex.ToString()
            };
        }
        finally
        {
            IsLoadingResponse = false;
        }
    }

    private static string? ExtractBetween(string html, string start, string end)
    {
        var s = html.IndexOf(start, StringComparison.OrdinalIgnoreCase);
        if (s < 0) return null;
        s += start.Length;
        var e = html.IndexOf(end, s, StringComparison.OrdinalIgnoreCase);
        return e < 0 ? null : html[s..e].Trim();
    }

    private static string? ExtractMetaContent(string html, string name)
    {
        var pattern = $"<meta[^>]*name=[\"']{name}[\"'][^>]*content=[\"']([^\"']*)[\"']";
        var match = System.Text.RegularExpressions.Regex.Match(html, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success) return match.Groups[1].Value;

        // Try reversed order (content before name)
        pattern = $"<meta[^>]*content=[\"']([^\"']*)[\"'][^>]*name=[\"']{name}[\"']";
        match = System.Text.RegularExpressions.Regex.Match(html, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
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

    private async void HealthCheck()
    {
        if (SelectedSite?.Url == null)
        {
            HealthCheckResult = "No URL available";
            return;
        }

        HealthCheckResult = "Checking...";
        try
        {
            using var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };
            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
            var sw = Stopwatch.StartNew();
            var response = await client.GetAsync(SelectedSite.Url);
            sw.Stop();

            var entry = new HealthCheckEntry(DateTime.Now, (int)response.StatusCode, sw.ElapsedMilliseconds);
            HealthCheckHistory = new ObservableCollection<HealthCheckEntry>(
                HealthCheckHistory.Append(entry).TakeLast(20));
            ResponseTimeValues = HealthCheckHistory
                .Select(h => (double)h.ResponseTimeMs)
                .ToList();

            HealthCheckResult = $"{(int)response.StatusCode} {response.StatusCode} — {sw.ElapsedMilliseconds}ms";
        }
        catch (TaskCanceledException)
        {
            HealthCheckResult = "Timeout (10s)";
        }
        catch (Exception ex)
        {
            HealthCheckResult = $"Error: {ex.Message}";
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

        // Read environment from web.config
        var currentEnv = ReadEnvironmentFromWebConfig(SelectedSite.PhysicalPath);
        SelectedSite.CurrentEnvironment = currentEnv;

        // Build available environments from appsettings files + defaults
        var envs = new System.Collections.Generic.List<string> { "Development", "Staging", "Production" };
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
        SelectedSite.AvailableEnvironments = new ObservableCollection<string>(
            envs.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(e => e));

        // Load appsettings based on environment
        var settingsFile = currentEnv.Equals("Production", StringComparison.OrdinalIgnoreCase)
            ? "appsettings.json"
            : $"appsettings.{currentEnv}.json";
        SelectedSite.SelectedAppSettingsFile = settingsFile;

        // Discover all appsettings*.json files
        try
        {
            var files = Directory.GetFiles(SelectedSite.PhysicalPath, "appsettings*.json")
                .Select(Path.GetFileName)
                .Where(f => f != null)
                .Cast<string>()
                .OrderBy(f => f)
                .ToList();

            SelectedSite.AppSettingsFiles = new ObservableCollection<string>(files);
        }
        catch
        {
            SelectedSite.AppSettingsFiles = new ObservableCollection<string>(["appsettings.json"]);
        }

        try
        {
            var appSettingsPath = Path.Combine(SelectedSite.PhysicalPath, settingsFile);
            if (!File.Exists(appSettingsPath))
                appSettingsPath = Path.Combine(SelectedSite.PhysicalPath, "appsettings.json");

            SelectedSite.AppSettingsContent = File.Exists(appSettingsPath)
                ? File.ReadAllText(appSettingsPath)
                : "// File not found";
        }
        catch (Exception ex)
        {
            SelectedSite.AppSettingsContent = $"// Error: {ex.Message}";
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

        LoadCertificates();
        LoadWorkerProcesses();
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
}