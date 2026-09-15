using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace TomLabs.IISBlitz.App.Converters;

/// <summary>Chip label for an appsettings file: "appsettings.json" → "base", "appsettings.Production.json" → "Production".</summary>
public sealed class AppSettingsLabelConverter : IValueConverter
{
    public static readonly AppSettingsLabelConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string name) return value;
        if (name.Equals("appsettings.json", StringComparison.OrdinalIgnoreCase)) return "base";
        if (name.StartsWith("appsettings.", StringComparison.OrdinalIgnoreCase) && name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            return name["appsettings.".Length..^".json".Length];
        return name;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
