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
            var exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
            if (exePath != null)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true,
                    Verb = "runas"
                });
                Close();
            }
        }
        catch (Exception)
        {
            // User cancelled the UAC prompt or launch failed
        }
    }
}