using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace TomLabs.IISBlitz.App.Converters;

/// <summary>
/// Maps a log / event level ("Error", "Warning", "Info") or a health status ("Healthy", "Degraded", "Unhealthy")
/// to the matching semantic brush from Tokens.axaml.
/// </summary>
public sealed class LevelToBrushConverter : IValueConverter
{
    public static readonly LevelToBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var level = value?.ToString()?.ToLowerInvariant() ?? string.Empty;
        var key = level switch
        {
            _ when level.StartsWith("err") || level.StartsWith("crit") || level.StartsWith("fatal") || level.StartsWith("unhealthy") => "ErrorBrush",
            _ when level.StartsWith("warn") || level.StartsWith("degraded") => "WarnBrush",
            _ when level.StartsWith("info") || level.StartsWith("log") || level.StartsWith("healthy") || level == "ok" => "RunningBrush",
            _ => "TextMutedBrush",
        };

        return Application.Current?.TryGetResource(key, Application.Current.ActualThemeVariant, out var brush) == true
            ? brush as IBrush
            : Brushes.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
