using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using NoteToolAvalonia.Models;

namespace NoteToolAvalonia.Converters;

public class PriorityColorConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is NotePriority priority)
		{
			return priority switch
			{
				NotePriority.Critical => new SolidColorBrush(Color.Parse("#ff4757")),
				NotePriority.High => new SolidColorBrush(Color.Parse("#ff6348")),
				NotePriority.Medium => new SolidColorBrush(Color.Parse("#ffa502")),
				NotePriority.Low => new SolidColorBrush(Color.Parse("#2ed573")),
				_ => new SolidColorBrush(Color.Parse("#57606f"))
			};
		}
		return new SolidColorBrush(Color.Parse("#57606f"));
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}