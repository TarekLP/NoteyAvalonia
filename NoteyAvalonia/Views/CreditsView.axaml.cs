using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using NoteToolAvalonia.ViewModels;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;

namespace NoteToolAvalonia.Views;

/// <summary>
/// Converts a URL string into an Avalonia Bitmap for use in Image controls.
/// Works for both local file paths and remote https:// URLs.
/// Can be reused in any project — just register it as a resource in your AXAML.
/// </summary>
public class UrlToBitmapConverter : IValueConverter
{
    private static readonly HttpClient _httpClient = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string url || string.IsNullOrWhiteSpace(url))
            return null;

        if (url.StartsWith("http://") || url.StartsWith("https://"))
        {
            try
            {
                var bytes = _httpClient.GetByteArrayAsync(url).GetAwaiter().GetResult();
                using var stream = new MemoryStream(bytes);
                return new Bitmap(stream);
            }
            catch
            {
                return null;
            }
        }

        if (File.Exists(url))
        {
            try { return new Bitmap(url); }
            catch { return null; }
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public partial class CreditsView : UserControl
{
    public CreditsView()
    {
        InitializeComponent();
    }

    private void CreditTile_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is CreditItem creditItem)
        {
            if (!string.IsNullOrEmpty(creditItem.Url))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = creditItem.Url,
                        UseShellExecute = true
                    });
                }
                catch { }
            }
        }
    }
}
