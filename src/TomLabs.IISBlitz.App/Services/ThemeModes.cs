using System;
using Avalonia;
using Avalonia.Styling;

namespace TomLabs.IISBlitz.App.Services;

/// <summary>
/// The three theme modes of the header switch. <see cref="System"/> leaves <c>RequestedThemeVariant</c> at
/// <c>Default</c>, so the palette follows Windows live (including Auto Dark Mode switching by time of day);
/// <see cref="Light"/> and <see cref="Dark"/> pin it. The switch cycles System → Light → Dark → System.
/// </summary>
public static class ThemeModes
{
    public const string System = "System";
    public const string Light = "Light";
    public const string Dark = "Dark";

    /// <summary>
    /// Version of the stored theme choice. Settings below it come from the old two-state toggle, which saved
    /// "Light"/"Dark" on every click; they are reset to <see cref="System"/> once.
    /// </summary>
    public const int CurrentVersion = 2;

    /// <summary>Maps a stored value (null, "Default", "System", "Light", "Dark") to one of the three modes.</summary>
    public static string Normalize(string? stored)
    {
        if (string.Equals(stored, Light, StringComparison.OrdinalIgnoreCase)) return Light;
        if (string.Equals(stored, Dark, StringComparison.OrdinalIgnoreCase)) return Dark;
        return System;
    }

    /// <summary>The mode the switch moves to next: System → Light → Dark → System.</summary>
    public static string Next(string? stored) => Normalize(stored) switch
    {
        System => Light,
        Light => Dark,
        _ => System,
    };

    public static ThemeVariant ToVariant(string? stored) => Normalize(stored) switch
    {
        Light => ThemeVariant.Light,
        Dark => ThemeVariant.Dark,
        _ => ThemeVariant.Default,
    };

    /// <summary>FontAwesome icon of the mode: a half circle for System, a sun for Light, a moon for Dark.</summary>
    public static string Icon(string? stored) => Normalize(stored) switch
    {
        Light => "fa-solid fa-sun",
        Dark => "fa-solid fa-moon",
        _ => "fa-solid fa-circle-half-stroke",
    };

    /// <summary>Applies the mode to the running application; System hands the choice back to the OS.</summary>
    public static void Apply(string? stored)
    {
        if (Application.Current is { } app)
            app.RequestedThemeVariant = ToVariant(stored);
    }
}
