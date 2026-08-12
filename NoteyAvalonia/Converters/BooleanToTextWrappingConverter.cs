using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace NoteToolAvalonia.Converters;

/// <summary>
/// Maps a bool to a TextWrapping value (true → Wrap, false → NoWrap).
/// Used by the markdown editor's word-wrap toggle.
/// </summary>
public class BooleanToTextWrappingConverter : IValueConverter
{
    public static readonly BooleanToTextWrappingConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? TextWrapping.Wrap : TextWrapping.NoWrap;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is TextWrapping tw && tw == TextWrapping.Wrap;
}
