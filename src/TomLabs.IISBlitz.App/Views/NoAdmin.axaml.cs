using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace TomLabs.IISBlitz.App.Views;

public partial class NoAdmin : Window
{
    public NoAdmin()
    {
        InitializeComponent();
    }

    private void OnRestartAsAdminClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var mainModule = Process.GetCurrentProcess().MainModule;
            var hostPath = mainModule?.FileName;

            if (hostPath == null)
                return;

            // When running via "dotnet run", the host is dotnet.exe — we need to pass the DLL as an argument
            var isDotnetHost = hostPath.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase)
                            || hostPath.EndsWith("dotnet", StringComparison.OrdinalIgnoreCase);

            if (isDotnetHost)
            {
                // Find the app DLL from command line args
                var args = Environment.GetCommandLineArgs();
                var dllPath = args.Length > 0 ? args[0] : null;

                if (dllPath != null)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = hostPath,
                        Arguments = $"\"{dllPath}\"",
                        UseShellExecute = true,
                        Verb = "runas"
                    });
                }
            }
            else
            {
                // Published single-file exe — just re-launch itself
                Process.Start(new ProcessStartInfo
                {
                    FileName = hostPath,
                    UseShellExecute = true,
                    Verb = "runas"
                });
            }

            Close();
        }
        catch (Exception)
        {
            // User cancelled the UAC prompt or launch failed
        }
    }
}