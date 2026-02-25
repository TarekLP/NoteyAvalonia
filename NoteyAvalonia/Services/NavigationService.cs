using NoteToolAvalonia.Models;
using NoteToolAvalonia.ViewModels;

namespace NoteToolAvalonia.Services;

public class NavigationService : INavigationService
{
    private MainWindowViewModel? _mainVm;

    public void SetMainViewModel(MainWindowViewModel vm)
    {
        _mainVm = vm;
    }

    public void NavigateToWelcome() => _mainVm?.NavigateToWelcome();
    public void NavigateToBoard(Board board) => _mainVm?.NavigateToBoard(board);
    public void NavigateToNoteEditor(NoteCard card, Board board, BoardColumn column) =>
        _mainVm?.NavigateToNoteEditor(card, board, column);
    public void NavigateToSettings() => _mainVm?.NavigateToSettings();
}
