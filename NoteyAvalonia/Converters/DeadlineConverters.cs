using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace NoteToolAvalonia.Converters;

/// <summary>
/// Returns true if a nullable DateTime has a value.
/// Used to show/hide the deadline pill on note cards.
/// </summary>
public class DeadlineVisibilityConverter : IValueConverter
{
    public static readonly DeadlineVisibilityConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is DateTime;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Formats a nullable DateTime into a human-readable deadline string.
/// Overdue dates are prefixed with "⚠ " so they stand out at a glance.
/// </summary>
public class DeadlineTextConverter : IValueConverter
{
    public static readonly DeadlineTextConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DateTime d) return string.Empty;
        var overdue = d.Date < DateTime.Today;
        return overdue ? $"⚠ {d:MMM dd}" : d.ToString("MMM dd");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Returns a background brush for the deadline pill.
/// Red tint if overdue, subtle accent tint if upcoming.
/// </summary>
public class DeadlineBgConverter : IValueConverter
{
    public static readonly DeadlineBgConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var overdue = value is true;
        return overdue
            ? new SolidColorBrush(Color.Parse("#33ff4757"))   // red tint
            : new SolidColorBrush(Color.Parse("#336a1b9a"));  // purple tint
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Returns a foreground colour for deadline text.
/// Red if overdue, accent purple if upcoming.
/// </summary>
public class DeadlineFgConverter : IValueConverter
{
    public static readonly DeadlineFgConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var overdue = value is true;
        return overdue
            ? new SolidColorBrush(Color.Parse("#ff4757"))
            : new SolidColorBrush(Color.Parse("#c792ea"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
