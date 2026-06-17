using Avalonia.Controls;
using Avalonia.Input;
using NoteToolAvalonia.Models;
using NoteToolAvalonia.ViewModels;

namespace NoteToolAvalonia.Views;

public partial class WelcomeView : UserControl
{
    public WelcomeView() { InitializeComponent(); }

    private void NoteCard_Tapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border b && b.DataContext is NoteCard card &&
            DataContext is WelcomeViewModel vm)
        {
            vm.OpenNoteCommand.Execute(card);
        }
    }
}
