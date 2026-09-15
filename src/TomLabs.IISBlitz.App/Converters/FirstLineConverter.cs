using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace TomLabs.IISBlitz.App.Converters;

/// <summary>Collapses a multi-line message to its first non-empty line for table cells; the detail pane shows the rest.</summary>
public sealed class FirstLineConverter : IValueConverter
{
    public static readonly FirstLineConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string text) return value;
        foreach (var line in text.Split('\n'))
        {
            var trimmed = line.Trim('\r', ' ', '\t');
            if (trimmed.Length > 0) return trimmed;
        }
        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
