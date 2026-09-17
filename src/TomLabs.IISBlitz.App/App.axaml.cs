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
            StartUpdater(desktop);

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
    
    /// <summary>Self-update from GitHub Releases: nightly builds follow the nightly pre-release, releases follow stable.</summary>
    private static void StartUpdater(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var settings = Services.UserSettings.Load();

        // IISBLITZ_UPDATE_MANIFEST=https://host/latest.json points the updater at a test manifest instead of GitHub.
        var manifestOverride = Environment.GetEnvironmentVariable("IISBLITZ_UPDATE_MANIFEST");
        TomLabs.AutoUpdate.IUpdateSource source = Uri.TryCreate(manifestOverride, UriKind.Absolute, out var manifestUri)
            ? new TomLabs.AutoUpdate.ManifestSource(manifestUri)
            : new TomLabs.AutoUpdate.GitHubReleasesSource("TomasBouda", "IISBlitz");

        TomLabs.AutoUpdate.Updater.Start(new TomLabs.AutoUpdate.UpdateOptions("IISBlitz", source)
        {
            Channel = Enum.TryParse<TomLabs.AutoUpdate.UpdateChannel>(settings.UpdateChannel, out var channel) ? channel : null,
            ChannelChanged = ch =>
            {
                var s = Services.UserSettings.Load();
                s.UpdateChannel = ch.ToString();
                s.Save();
            },
            ExitApplication = () => desktop.Shutdown(),
            // Manifests must be signed by the CI key (UPDATE_SIGNING_KEY secret); this is the matching public key.
            PublicKeyPem = """
                -----BEGIN PUBLIC KEY-----
                MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEr7DrQwulUeVzVelk3L9CM39ATGc7
                lnm1GwBK2g2HHRj+NF8Tq1sRToA4BfS3sHWJsUci0+LClMnhk8LtOBdnOw==
                -----END PUBLIC KEY-----
                """,
            Log = Services.AppLog.Write,
        });
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