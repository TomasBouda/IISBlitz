using System;
using System.Collections.Generic;
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

    /// <summary>"Dark" or "Light"; null follows the operating system preference live.</summary>
    public string? Theme { get; set; }

    /// <summary>Version of <see cref="Theme"/>; see <see cref="ThemeModes.CurrentVersion"/>.</summary>
    public int ThemeVersion { get; set; }

    /// <summary>"Stable" or "Nightly"; null follows the channel of the running build.</summary>
    public string? UpdateChannel { get; set; }

    /// <summary>Extra files opened as tabs, per site name, so they come back next time the site is selected.</summary>
    public Dictionary<string, List<string>> OpenFiles { get; set; } = new();

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

    /// <summary>
    /// One-time reset of a Light/Dark choice saved by the old two-state toggle back to System, so every
    /// installation follows Windows again; a choice made with the three-state switch is kept.
    /// </summary>
    public void MigrateTheme()
    {
        if (ThemeVersion >= ThemeModes.CurrentVersion)
            return;

        var reset = Theme is not null;
        Theme = null;
        ThemeVersion = ThemeModes.CurrentVersion;
        if (reset)
            Save();
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
