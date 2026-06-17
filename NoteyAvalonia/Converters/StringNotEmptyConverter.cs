using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace NoteToolAvalonia.Converters;

/// <summary>
/// Returns true if a string is non-null and non-whitespace.
/// Useful for showing/hiding UI elements based on whether a field has content.
/// Drop-in reusable — works in any Avalonia project.
/// </summary>
public class StringNotEmptyConverter : IValueConverter
{
    /// Static instance so AXAML can reference it without a resource entry:
    /// {x:Static converters:StringNotEmptyConverter.Instance}
    public static readonly StringNotEmptyConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string s && !string.IsNullOrWhiteSpace(s);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
