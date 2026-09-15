using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TomLabs.IISBlitz.App.Services;

/// <summary>
/// Per-user preferences persisted under %APPDATA%\IISBlitz\settings.json.
/// </summary>
public sealed class UserSettings
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "IISBlitz", "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>"Dark" or "Light"; null follows the operating system preference.</summary>
    public string? Theme { get; set; }

    /// <summary>"Stable" or "Nightly"; null follows the channel of the running build.</summary>
    public string? UpdateChannel { get; set; }

    public static UserSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
                return JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(SettingsPath), JsonOptions) ?? new UserSettings();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to read user settings: {ex.Message}");
        }

        return new UserSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to write user settings: {ex.Message}");
        }
    }
}
