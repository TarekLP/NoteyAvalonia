using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NoteToolAvalonia.Models;
using NoteToolAvalonia.Services;

namespace NoteToolAvalonia.ViewModels;

public partial class WelcomeViewModel : ViewModelBase
{
    private readonly NavigationService _navigation;
    private readonly DataService _dataService;

    [ObservableProperty]
    private ObservableCollection<Board> _boards = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<Board> _filteredBoards = new();

    [ObservableProperty]
    private string _newBoardName = string.Empty;

    [ObservableProperty]
    private bool _isCreatingBoard;

    public WelcomeViewModel(NavigationService navigation, DataService dataService)
    {
        _navigation = navigation;
        _dataService = dataService;
        LoadBoards();
    }

    private void LoadBoards()
    {
        var boards = _dataService.LoadBoards();
        Boards = new ObservableCollection<Board>(boards.OrderByDescending(b => b.LastModified));
        ApplyFilter();
        OnPropertyChanged(nameof(TotalBoards));
        OnPropertyChanged(nameof(TotalNotes));
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
            FilteredBoards = new ObservableCollection<Board>(Boards);
        else
            FilteredBoards = new ObservableCollection<Board>(
                Boards.Where(b =>
                    b.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    b.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase)));
    }

    [RelayCommand]
    private void OpenBoard(Board board)
    {
        board.LastModified = DateTime.Now;
        _dataService.SaveBoard(board);
        _navigation.NavigateToBoard(board);
    }

    [RelayCommand]
    private void ShowCreateBoard()
    {
        IsCreatingBoard = true;
        NewBoardName = string.Empty;
    }

    [RelayCommand]
    private void CancelCreateBoard()
    {
        IsCreatingBoard = false;
        NewBoardName = string.Empty;
    }

    [RelayCommand]
    private void CreateBoard()
    {
        if (string.IsNullOrWhiteSpace(NewBoardName)) return;
        var board = new Board
        {
            Name = NewBoardName.Trim(),
            Columns = new()
            {
                new BoardColumn { Title = "To Do",       Order = 0, Color = "#3498db" },
                new BoardColumn { Title = "In Progress", Order = 1, Color = "#f39c12" },
                new BoardColumn { Title = "Done",        Order = 2, Color = "#2ecc71" }
            }
        };
        _dataService.SaveBoard(board);
        IsCreatingBoard = false;
        NewBoardName = string.Empty;
        LoadBoards();
    }

    [RelayCommand]
    private void DeleteBoard(Board board)
    {
        _dataService.DeleteBoard(board.Id);
        LoadBoards();
    }

    [RelayCommand]
    private void OpenSettings() => _navigation.NavigateToSettings();

    public int TotalNotes => Boards.Sum(b => b.Columns.Sum(c => c.Cards.Count));
    public int TotalBoards => Boards.Count;
}
