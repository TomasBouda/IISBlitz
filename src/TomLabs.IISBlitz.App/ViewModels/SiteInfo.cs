using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using TomLabs.IISBlitz.App.Models;

namespace TomLabs.IISBlitz.App.ViewModels;

public partial class SiteInfo : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private bool _isPoolRunning;

    [ObservableProperty]
    private string _appPool = string.Empty;

    [ObservableProperty]
    private string _physicalPath = string.Empty;

    [ObservableProperty]
    private string? _appSettingsContent;

    [ObservableProperty]
    private string? _webConfigContent;

    [ObservableProperty]
    private ObservableCollection<string>? _logs;

    [ObservableProperty]
    private ObservableCollection<BindingInfo> _bindings = new();

    [ObservableProperty]
    private string? _selectedLogContent;

    [ObservableProperty]
    private string? _selectedLogPath;

    [ObservableProperty]
    private ObservableCollection<WorkerProcessInfo> _workerProcesses = new();

    [ObservableProperty]
    private ObservableCollection<CertificateInfo> _certificates = new();

    [ObservableProperty]
    private ObservableCollection<FolderPermissionEntry> _permissions = new();

    [ObservableProperty]
    private ObservableCollection<string> _appSettingsFiles = new();

    [ObservableProperty]
    private string? _selectedAppSettingsFile;

    [ObservableProperty]
    private string _currentEnvironment = "Production";

    /// <summary>What web.config actually says: the environment name, or "not set → Production".</summary>
    [ObservableProperty]
    private string _configuredEnvironmentText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _availableEnvironments = new();

    public string? Url => Bindings?.FirstOrDefault() is { } b
        ? $"{b.Protocol}://{(string.IsNullOrEmpty(b.Host) ? "localhost" : b.Host)}:{b.Port}"
        : null;

    /// <summary>Sidebar subtitle: bound ports followed by the app pool, e.g. ":80 :443 · DefaultAppPool".</summary>
    public string Summary
    {
        get
        {
            var ports = string.Join(" ", Bindings.Select(b => $":{b.Port}").Distinct());
            return string.IsNullOrEmpty(ports) ? AppPool : $"{ports} · {AppPool}";
        }
    }

    partial void OnBindingsChanged(ObservableCollection<BindingInfo> value)
    {
        OnPropertyChanged(nameof(Summary));
        OnPropertyChanged(nameof(Url));
    }

    partial void OnAppPoolChanged(string value) => OnPropertyChanged(nameof(Summary));

    public override string ToString() => Name;
}