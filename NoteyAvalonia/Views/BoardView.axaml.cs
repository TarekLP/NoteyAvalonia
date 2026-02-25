using Avalonia.Controls;
using Avalonia.Input;
using NoteToolAvalonia.Models;
using NoteToolAvalonia.ViewModels;

namespace NoteToolAvalonia.Views;

public partial class BoardView : UserControl
{
    public BoardView() { InitializeComponent(); }

    private void NoteCard_Tapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border border && border.DataContext is NoteCard card)
        {
            if (DataContext is BoardViewModel vm)
                vm.OpenCardCommand.Execute(card);
        }
    }
}
