using NoteToolAvalonia.Models;

namespace NoteToolAvalonia.Services;

public interface INavigationService
{
    void NavigateToWelcome();
    void NavigateToBoard(Board board);
    void NavigateToNoteEditor(NoteCard card, Board board, BoardColumn column);
    void NavigateToSettings();
}
