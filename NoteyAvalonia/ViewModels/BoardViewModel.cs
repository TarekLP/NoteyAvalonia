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
	public void AddColumn()
	{
		if (string.IsNullOrWhiteSpace(NewColumnTitle)) return;

		var newColumn = new BoardColumn
		{
			Title = NewColumnTitle.Trim(),
			Order = Columns.Count,
			Color = "#3498db"
		};

		Columns.Add(newColumn);
		Save();
		NewColumnTitle = string.Empty;
		IsAddingColumn = false;
	}

	[RelayCommand]
	public void AddCard(BoardColumn column)
	{
		var newCard = new NoteCard
		{
			Title = "New Note",
			ColumnId = column.Id,
			Priority = NotePriority.None,
			CreatedAt = DateTime.Now,
			LastModified = DateTime.Now
		};

		column.Cards.Add(newCard);
		Save();

		_navigation.NavigateToNoteEditor(newCard, Board, column);
	}

	public void MoveCardToColumn(NoteCard card, BoardColumn targetColumn)
	{
		var sourceCol = Columns.FirstOrDefault(c => c.Id == card.ColumnId);
		if (sourceCol != null)
		{
			sourceCol.Cards.Remove(card);
		}
		card.ColumnId = targetColumn.Id;
		targetColumn.Cards.Add(card);
		Save();
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
		var column = Columns.FirstOrDefault(c => c.Id == card.ColumnId);
		if (column != null)
		{
			_navigation.NavigateToNoteEditor(card, Board, column);
		}
	}

	[RelayCommand]
	public void CancelEditBoardName()
	{
		IsEditingBoardName = false;
	}

	[RelayCommand]
	public void OpenCard(NoteCard card)
	{
		var column = Columns.FirstOrDefault(c => c.Id == card.ColumnId);
		if (column != null)
		{
			_navigation.NavigateToNoteEditor(card, Board, column);
		}
	}

	[RelayCommand]
	public void DeleteColumn(BoardColumn column)
	{
		if (column == null) return;
		Columns.Remove(column);
		Save();
	}

	[RelayCommand]
	public void DeleteCard(NoteCard card)
	{
		if (card == null) return;
		var column = Columns.FirstOrDefault(c => c.Id == card.ColumnId);
		if (column != null)
		{
			column.Cards.Remove(card);
			Save();
		}
	}
}