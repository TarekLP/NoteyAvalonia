using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;

namespace NoteToolAvalonia.Views;

/// <summary>
/// Small code-only dialog helpers used by the ViewModels. Avoids pulling
/// in a third-party dialog package — a simple styled confirmation window.
/// </summary>
public static class Dialogs
{
    public static async Task<bool> ConfirmAsync(
        string title, string message,
        string confirmText = "Delete", string cancelText = "Cancel")
    {
        var lifetime = Application.Current?.ApplicationLifetime;
        var owner = lifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } w } ? w : null;
        if (owner == null) return true;

        var result = false;

        var dialog = new Window
        {
            Title = title,
            Width = 430,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            ShowInTaskbar = false,
            Background = (IBrush?)Application.Current?.Resources["Brush.Surface"]
        };

        var messageText = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(20, 20, 20, 16),
            FontSize = 14
        };

        var confirmButton = new Button
        {
            Content = confirmText,
            Classes = { "danger" },
            Width = 96,
            Margin = new Thickness(0, 0, 8, 0),
            Padding = new Thickness(0, 6, 0, 6)
        };
        confirmButton.Click += (_, _) => { result = true; dialog.Close(result); };

        var cancelButton = new Button
        {
            Content = cancelText,
            Classes = { "ghost" },
            Width = 96,
            Padding = new Thickness(0, 6, 0, 6)
        };
        cancelButton.Click += (_, _) => dialog.Close(false);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(20, 0, 20, 20),
            Children = { confirmButton, cancelButton }
        };

        dialog.Content = new StackPanel
        {
            Children = { messageText, buttons }
        };

        return await dialog.ShowDialog<bool>(owner);
    }
}
