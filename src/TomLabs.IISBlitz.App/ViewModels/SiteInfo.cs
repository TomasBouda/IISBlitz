using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TomLabs.IISBlitz.App.ViewModels;

public partial class SiteInfo : ObservableObject
{
    [ObservableProperty]
    public string _name;

    [ObservableProperty]
    public bool _isRunning;
        
    [ObservableProperty]
    public bool _isPoolRunning;

    [ObservableProperty] 
    public string _appPool;
        
    [ObservableProperty]
    public string _physicalPath;

    [ObservableProperty] 
    public string _appSettingsContent;
    
    [ObservableProperty] 
    public string _webConfigContent;

    [ObservableProperty]
    public string _favicon;
    
    [ObservableProperty]
    public ObservableCollection<string>? _logs;

    public override string ToString()
    {
        return $"{Name}";
    }
}