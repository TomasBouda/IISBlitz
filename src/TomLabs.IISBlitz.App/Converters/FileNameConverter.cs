using System;
using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;

namespace TomLabs.IISBlitz.App.Converters;

/// <summary>Shows only the file name of a full path (the path itself stays available in a tooltip).</summary>
public sealed class FileNameConverter : IValueConverter
{
    public static readonly FileNameConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string path && path.Length > 0 ? Path.GetFileName(path) : value;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
