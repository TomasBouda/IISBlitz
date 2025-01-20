using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Web.Administration;
using ReactiveUI;

namespace TomLabs.IISBlitz.App.ViewModels;

public partial class SiteViewModel : ObservableObject
{
    private ServerManager _serverManager;
    
    [ObservableProperty]
    public ObservableCollection<SiteInfo> _siteList = new ObservableCollection<SiteInfo>();

    private SiteInfo? _selectedSite;
    public SiteInfo? SelectedSite
    {
        get => _selectedSite;
        set
        {
            if (_selectedSite != value)
            {
                _selectedSite = value;
                OnPropertyChanged(); // Ensure the property change is detected
                LoadSiteDetails(); // Load additional data when SelectedSite changes
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

    public SiteViewModel()
    {
        StartWebCmd = ReactiveCommand.Create(StartWebsite);
        StopWebCmd = ReactiveCommand.Create(StopWebsite);
        StartPoolCmd = ReactiveCommand.Create(StartAppPool);
        StopPoolCmd = ReactiveCommand.Create(StopAppPool);

        OpenAppSettingsCmd = ReactiveCommand.Create(OpenSiteAppSettings);
        OpenWebConfigCmd = ReactiveCommand.Create(OpenSiteWebConfig);
        OpenWebFolderCmd = ReactiveCommand.Create(OpenWebFolder);

        _serverManager = new ServerManager();
        LoadIISSites();
    }
    
    private void StartWebsite()
        {
            if (SelectedSite != null)
            {
                var site = _serverManager.Sites[SelectedSite.Name];
                site.Start();
                LoadIISSites();  // Refresh the list after stopping the site
            }
        } 
        
        private void StopWebsite()
        {
            if (SelectedSite != null)
            {
                var site = _serverManager.Sites[SelectedSite.Name];
                site.Stop();
                LoadIISSites();  // Refresh the list after stopping the site
            }
        }

        private void StartAppPool()
        {
            if (SelectedSite != null)
            {
                var appPool = _serverManager.ApplicationPools[SelectedSite.AppPool];
                appPool.Start();
                LoadIISSites();  // Refresh the list after stopping the app pool
            }
        }
        
        private void StopAppPool()
        {
            if (SelectedSite != null)
            {
                var appPool = _serverManager.ApplicationPools[SelectedSite.AppPool];
                appPool.Stop();
                LoadIISSites();  // Refresh the list after stopping the app pool
            }
        }

        private void OpenWebFolder()
        {
            if (SelectedSite != null)
            {
                Process.Start(new System.Diagnostics.ProcessStartInfo() {
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
                LoadIISSites();  
            }
        }
        
        private void OpenSiteWebConfig()
        {
            if (SelectedSite != null)
            {
                OpenFileInDefaultEditor(Path.Combine(SelectedSite.PhysicalPath, "web.config"));
                LoadIISSites();  
            }
        }
        
        private void OpenFileInDefaultEditor(string filePath)
        {
            if (File.Exists(filePath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true // This opens the file with the default associated application
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
                    if (existingSite == null)
                    {
                        var appPool = _serverManager.ApplicationPools[site.Applications[0].ApplicationPoolName];
                        var sitePath = site.Applications[0].VirtualDirectories[0].PhysicalPath;
                        sitePath = Environment.ExpandEnvironmentVariables(sitePath);

                        SiteList.Add(new SiteInfo
                        {
                            Name = site.Name,
                            IsRunning = site.State == ObjectState.Started,
                            IsPoolRunning = appPool.State == ObjectState.Started,
                            AppPool = site.Applications[0].ApplicationPoolName,
                            PhysicalPath = sitePath,
                        });
                    }
                    else
                    {
                        var appPool = _serverManager.ApplicationPools[site.Applications[0].ApplicationPoolName];
                        existingSite.IsRunning = site.State == ObjectState.Started;
                        existingSite.IsPoolRunning = appPool.State == ObjectState.Started;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }
        
        private void LoadSiteDetails()
        {
            if (SelectedSite != null)
            {
                SelectedSite.AppSettingsContent = File.ReadAllText(Path.Combine(SelectedSite.PhysicalPath, "appsettings.json"));
                SelectedSite.WebConfigContent = File.ReadAllText(Path.Combine(SelectedSite.PhysicalPath, "web.config"));
            }
        }
}