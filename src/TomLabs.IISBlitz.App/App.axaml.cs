using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using System.Security.Principal;
using Avalonia.Markup.Xaml;
using TomLabs.IISBlitz.App.ViewModels;
using TomLabs.IISBlitz.App.Views;

namespace TomLabs.IISBlitz.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        ApplySavedTheme();
    }

    /// <summary>Restores the theme the user picked last time; without a saved choice the OS preference applies.</summary>
    private void ApplySavedTheme()
    {
        var settings = Services.UserSettings.Load();
        RequestedThemeVariant = settings.Theme switch
        {
            "Dark" => Avalonia.Styling.ThemeVariant.Dark,
            "Light" => Avalonia.Styling.ThemeVariant.Light,
            _ => Avalonia.Styling.ThemeVariant.Default,
        };
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // An exception in a command or event handler should not take the whole admin tool down.
        Avalonia.Threading.Dispatcher.UIThread.UnhandledException += (_, e) =>
        {
            Program.WriteCrashLog(e.Exception);
            e.Handled = true;
        };

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();

            if (IsRunningAsAdministrator())
            {
                try
                {
                    desktop.MainWindow = new MainWindow
                    {
                        DataContext = new MainWindowViewModel(),
                    };
                }
                catch (Exception ex)
                {
                    desktop.MainWindow = new NoAdmin();
                    Console.Error.WriteLine($"Failed to initialize: {ex}");
                }
            }
            else
            {
                desktop.MainWindow = new NoAdmin();
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
    
    private static bool IsRunningAsAdministrator()
    {
        // Only check if we're on Windows
        // (OperatingSystem.IsWindows requires .NET 6+)
        if (!OperatingSystem.IsWindows())
            return false;
            
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}