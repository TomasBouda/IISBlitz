using System;
using System.IO;

namespace TomLabs.IISBlitz.App.Services;

/// <summary>Plain append-only log in %APPDATA%\IISBlitz\iisblitz.log for things that would otherwise vanish with stderr.</summary>
public static class AppLog
{
    public static string Path { get; } = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "IISBlitz", "iisblitz.log");

    private static readonly object Gate = new();

    public static void Write(string message)
    {
        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
                // Keep the file from growing forever: start over past ~2 MB.
                if (File.Exists(Path) && new FileInfo(Path).Length > 2 * 1024 * 1024)
                    File.Delete(Path);
                File.AppendAllText(Path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // Logging must never take the app down.
        }
    }
}
