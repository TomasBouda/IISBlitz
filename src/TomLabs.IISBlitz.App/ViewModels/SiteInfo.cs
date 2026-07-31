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
    private ObservableCollection<string> _appSettingsFiles = new();

    [ObservableProperty]
    private string? _selectedAppSettingsFile;

    [ObservableProperty]
    private string _currentEnvironment = "Production";

    [ObservableProperty]
    private ObservableCollection<string> _availableEnvironments = new();

    public string? Url => Bindings?.FirstOrDefault() is { } b
        ? $"{b.Protocol}://{(string.IsNullOrEmpty(b.Host) ? "localhost" : b.Host)}:{b.Port}"
        : null;

    public override string ToString() => Name;
}