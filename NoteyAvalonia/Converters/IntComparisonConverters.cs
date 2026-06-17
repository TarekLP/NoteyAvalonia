using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace NoteToolAvalonia.Converters;

/// <summary>
/// Returns true if an integer equals the ConverterParameter.
/// Used in the note creator overlay to show/hide each step panel.
///
/// Example in AXAML:
///   IsVisible="{Binding CreatorStep,
///     Converter={x:Static converters:IntEqualsConverter.Instance},
///     ConverterParameter=2}"
/// </summary>
public class IntEqualsConverter : IValueConverter
{
    public static readonly IntEqualsConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int i && parameter is string s && int.TryParse(s, out var p))
            return i == p;
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Returns true if an integer is greater than the ConverterParameter.
/// Used to show the "Back" button only on steps beyond the first.
/// </summary>
public class IntGreaterThanConverter : IValueConverter
{
    public static readonly IntGreaterThanConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int i && parameter is string s && int.TryParse(s, out var p))
            return i > p;
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Returns true if an integer is less than the ConverterParameter.
/// Used to show the "Next" button only on steps before the last.
/// </summary>
public class IntLessThanConverter : IValueConverter
{
    public static readonly IntLessThanConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int i && parameter is string s && int.TryParse(s, out var p))
            return i < p;
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
