using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NoteToolAvalonia.Models;
using NoteToolAvalonia.Services;

namespace NoteToolAvalonia.ViewModels;

public partial class BoardViewModel : ViewModelBase
{
    private readonly NavigationService _navigation;
    private readonly DataService _dataService;

    [ObservableProperty]
    private Board _board;

    [ObservableProperty]
    private ObservableCollection<BoardColumn> _columns = new();

    [ObservableProperty]
    private string _newColumnTitle = string.Empty;

    [ObservableProperty]
    private bool _isAddingColumn;

    [ObservableProperty]
    private bool _isEditingBoardName;

    [ObservableProperty]
    private string _editBoardName = string.Empty;

    public BoardViewModel(Board board, NavigationService navigation, DataService dataService)
    {
        _navigation = navigation;
        _dataService = dataService;
        _board = board;
        Columns = new ObservableCollection<BoardColumn>(board.Columns.OrderBy(c => c.Order));
    }

    private void Save()
    {
        Board.Columns = Columns.ToList();
        Board.LastModified = DateTime.Now;
        _dataService.SaveBoard(Board);
    }

    [RelayCommand]
    private void ShowAddColumn()
    {
        IsAddingColumn = true;
        NewColumnTitle = string.Empty;
    }

    [RelayCommand]
    private void CancelAddColumn() => IsAddingColumn = false;

    [RelayCommand]
    private void AddColumn()
    {
        if (string.IsNullOrWhiteSpace(NewColumnTitle)) return;
        Columns.Add(new BoardColumn
        {
            Title = NewColumnTitle.Trim(),
            Order = Columns.Count,
            Color = "#3498db"
        });
        IsAddingColumn = false;
        NewColumnTitle = string.Empty;
        Save();
    }

    [RelayCommand]
    private void DeleteColumn(BoardColumn column)
    {
        Columns.Remove(column);
        for (int i = 0; i < Columns.Count; i++) Columns[i].Order = i;
        Save();
    }

    [RelayCommand]
    private void AddCard(BoardColumn column)
    {
        var card = new NoteCard { Title = "New Note", ColumnId = column.Id };
        column.Cards.Add(card);
        Save();
        var idx = Columns.IndexOf(column);
        Columns.RemoveAt(idx);
        Columns.Insert(idx, column);
        _navigation.NavigateToNoteEditor(card, Board, column);
    }

    [RelayCommand]
    private void OpenCard(NoteCard card)
    {
        var column = Columns.FirstOrDefault(c => c.Cards.Any(n => n.Id == card.Id));
        if (column != null) _navigation.NavigateToNoteEditor(card, Board, column);
    }

    [RelayCommand]
    private void DeleteCard(NoteCard card)
    {
        foreach (var col in Columns)
        {
            if (col.Cards.RemoveAll(c => c.Id == card.Id) > 0) break;
        }
        Save();
        RefreshColumns();
    }

    [RelayCommand]
    private void MoveCardLeft(NoteCard card) => MoveCard(card, -1);

    [RelayCommand]
    private void MoveCardRight(NoteCard card) => MoveCard(card, 1);

    private void MoveCard(NoteCard card, int direction)
    {
        var sourceCol = Columns.FirstOrDefault(c => c.Cards.Any(n => n.Id == card.Id));
        if (sourceCol == null) return;
        var sourceIdx = Columns.IndexOf(sourceCol);
        var targetIdx = sourceIdx + direction;
        if (targetIdx < 0 || targetIdx >= Columns.Count) return;
        var targetCol = Columns[targetIdx];
        sourceCol.Cards.RemoveAll(c => c.Id == card.Id);
        card.ColumnId = targetCol.Id;
        targetCol.Cards.Add(card);
        Save();
        RefreshColumns();
    }

    [RelayCommand]
    private void ToggleCardCompleted(NoteCard card)
    {
        card.IsCompleted = !card.IsCompleted;
        card.LastModified = DateTime.Now;
        Save();
        RefreshColumns();
    }

    private void RefreshColumns()
    {
        Columns = new ObservableCollection<BoardColumn>(Columns.ToList());
    }

    [RelayCommand]
    private void StartEditBoardName()
    {
        EditBoardName = Board.Name;
        IsEditingBoardName = true;
    }

    [RelayCommand]
    private void SaveBoardName()
    {
        if (!string.IsNullOrWhiteSpace(EditBoardName))
        {
            Board.Name = EditBoardName.Trim();
            Save();
        }
        IsEditingBoardName = false;
    }

    [RelayCommand]
    private void CancelEditBoardName() => IsEditingBoardName = false;

    [RelayCommand]
    private void GoBack() => _navigation.NavigateToWelcome();
}
