using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace NoteToolAvalonia.Converters;

public class BooleanToFontWeightConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b && b) return FontWeight.Bold;
        return FontWeight.Normal;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
