using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NoteToolAvalonia.Models;
using NoteToolAvalonia.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace NoteToolAvalonia.ViewModels;

public partial class BoardViewModel : ViewModelBase
{
	public readonly NavigationService _navigation;
	public readonly DataService _dataService;
	[ObservableProperty]
	public Board _board;

	[ObservableProperty]
	public ObservableCollection<BoardColumn> _columns = new();

	[ObservableProperty]
	public string _newColumnTitle = string.Empty;

	[ObservableProperty]
	public bool _isAddingColumn;

	[ObservableProperty]
	public bool _isEditingBoardName;

	[ObservableProperty]
	public string _editBoardName = string.Empty;

	public BoardViewModel(Board board, NavigationService navigation, DataService dataService)
	{
		_navigation = navigation;
		_dataService = dataService;
		_board = board;
		Columns = new ObservableCollection<BoardColumn>(board.Columns.OrderBy(c => c.Order));
	}

	public void Save()
	{
		Board.Columns = Columns.ToList();
		Board.LastModified = DateTime.Now;
		_dataService.SaveBoard(Board);
	}

	[RelayCommand]
	public void ShowAddColumn()
	{
		IsAddingColumn = true;
		NewColumnTitle = string.Empty;
	}

	[RelayCommand]
	public void CancelAddColumn() => IsAddingColumn = false;

	[RelayCommand]
	public void AddColumn()
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
	public void DeleteColumn(BoardColumn column)
	{
		Columns.Remove(column);
		for (int i = 0; i < Columns.Count; i++) Columns[i].Order = i;
		Save();
	}

	[RelayCommand]
	public void AddNote()
	{
		var firstColumn = Columns.FirstOrDefault();
		if (firstColumn != null)
		{
			AddCard(firstColumn);
		}
	}

	[RelayCommand]
	public void AddCard(BoardColumn column)
	{
		var card = new NoteCard { Title = "New Note", ColumnId = column.Id };
		column.Cards.Add(card);
		Save();
		RefreshColumns();
		_navigation.NavigateToNoteEditor(card, Board, column);
	}

	[RelayCommand]
	public void OpenCard(NoteCard card)
	{
		var column = Columns.FirstOrDefault(c => c.Cards.Any(n => n.Id == card.Id));
		if (column != null) _navigation.NavigateToNoteEditor(card, Board, column);
	}

	[RelayCommand]
	public void DeleteCard(NoteCard card)
	{
		foreach (var col in Columns)
		{
			var toRemove = col.Cards.FirstOrDefault(c => c.Id == card.Id);
			if (toRemove != null)
			{
				col.Cards.Remove(toRemove);
				_dataService.NoteFiles.DeleteNote(card.Id);
				break;
			}
		}
		Save();
		RefreshColumns();
	}

	[RelayCommand]
	public void MoveCardLeft(NoteCard card) => MoveCard(card, -1);

	[RelayCommand]
	public void MoveCardRight(NoteCard card) => MoveCard(card, 1);

	public void MoveCard(NoteCard card, int direction)
	{
		var sourceCol = Columns.FirstOrDefault(c => c.Cards.Any(n => n.Id == card.Id));
		if (sourceCol == null) return;
		var sourceIdx = Columns.IndexOf(sourceCol);
		var targetIdx = sourceIdx + direction;
		if (targetIdx < 0 || targetIdx >= Columns.Count) return;
		var targetCol = Columns[targetIdx];
		var toRemove = sourceCol.Cards.FirstOrDefault(c => c.Id == card.Id);
		if (toRemove != null)
			sourceCol.Cards.Remove(toRemove);
		card.ColumnId = targetCol.Id;
		targetCol.Cards.Add(card);
		Save();
		RefreshColumns();
	}

	[RelayCommand]
	public void ToggleCardCompleted(NoteCard card)
	{
		card.IsCompleted = !card.IsCompleted;
		card.LastModified = DateTime.Now;
		Save();
		RefreshColumns();
	}

	public void RefreshColumns()
	{
		Columns = new ObservableCollection<BoardColumn>(Columns.ToList());
	}

	[RelayCommand]
	public void StartEditBoardName()
	{
		EditBoardName = Board.Name;
		IsEditingBoardName = true;
	}

	[RelayCommand]
	public void SaveBoardName()
	{
		if (!string.IsNullOrWhiteSpace(EditBoardName))
		{
			Board.Name = EditBoardName.Trim();
			Save();
		}
		IsEditingBoardName = false;
	}

	[RelayCommand]
	public void EditCard(NoteCard card)
	{
		OpenCard(card);
	}

	[RelayCommand]
	public void CancelEditBoardName() => IsEditingBoardName = false;

	[RelayCommand]
	public void GoBack() => _navigation.NavigateToWelcome();

	public void MoveCardToColumn(NoteCard card, BoardColumn targetColumn)
	{
		var sourceColumn = Columns.FirstOrDefault(c => c.Cards.Contains(card));
		if (sourceColumn == null || sourceColumn == targetColumn) return;

		sourceColumn.Cards.Remove(card);
		card.ColumnId = targetColumn.Id;
		targetColumn.Cards.Add(card);
		Save();
		RefreshColumns();
	}
}