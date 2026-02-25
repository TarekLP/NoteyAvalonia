using Avalonia.Controls;
using Avalonia.Input;
using NoteToolAvalonia.Models;
using NoteToolAvalonia.ViewModels;

namespace NoteToolAvalonia.Views;

public partial class WelcomeView : UserControl
{
    public WelcomeView() { InitializeComponent(); }

    private void BoardCard_Tapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border border && border.DataContext is Board board)
        {
            if (DataContext is WelcomeViewModel vm)
                vm.OpenBoardCommand.Execute(board);
        }
    }
}
