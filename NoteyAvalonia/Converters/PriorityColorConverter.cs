using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using NoteToolAvalonia.Models;

namespace NoteToolAvalonia.Converters;

public class PriorityColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is NotePriority priority)
        {
            return priority switch
            {
                NotePriority.Critical => new SolidColorBrush(Color.Parse("#e74c3c")),
                NotePriority.High     => new SolidColorBrush(Color.Parse("#e67e22")),
                NotePriority.Medium   => new SolidColorBrush(Color.Parse("#f39c12")),
                NotePriority.Low      => new SolidColorBrush(Color.Parse("#2ecc71")),
                _                     => new SolidColorBrush(Color.Parse("#95a5a6"))
            };
        }
        return new SolidColorBrush(Color.Parse("#95a5a6"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
