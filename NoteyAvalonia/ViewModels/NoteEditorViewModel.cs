using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NoteToolAvalonia.Models;
using NoteToolAvalonia.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NoteToolAvalonia.ViewModels;

public partial class NoteEditorViewModel : ViewModelBase
{
	private readonly NavigationService _navigation;
	private readonly DataService _dataService;
	private readonly Board _board;
	private readonly NoteCard _card;
	private readonly EditHistoryService _editHistory = new();
	private System.Threading.CancellationTokenSource? _autoSaveCancellation;
	private int _autoSaveDelayMs = 2000;

	[ObservableProperty] private string _noteTitle = string.Empty;
	[ObservableProperty] private string _noteContent = string.Empty;
	[ObservableProperty] private int _wordCount;
	[ObservableProperty] private int _characterCount;
	[ObservableProperty] private int _lineCount;
	[ObservableProperty] private bool _hasUnsavedChanges;
	[ObservableProperty] private bool _isPreviewVisible = true;
	[ObservableProperty] private bool _isFullscreen;
	[ObservableProperty] private NotePriority _selectedPriority;
	[ObservableProperty] private string _tags = string.Empty;
	[ObservableProperty] private DateTime _createdAt;
	[ObservableProperty] private DateTime _lastModified;
	[ObservableProperty] private string _statusMessage = string.Empty;
	[ObservableProperty] private bool _showLineNumbers = false;
	[ObservableProperty] private bool _isWordWrapEnabled = true;
	[ObservableProperty] private string _findText = string.Empty;
	[ObservableProperty] private string _replaceText = string.Empty;
	[ObservableProperty] private int _findMatchCount = 0;
	[ObservableProperty] private bool _showFindReplace = false;

	public NoteEditorViewModel(NoteCard card, Board board, BoardColumn column, NavigationService navigation, DataService dataService)
	{
		_card = card;
		_board = board;
		_navigation = navigation;
		_dataService = dataService;

		NoteTitle = card.Title;
		NoteContent = _dataService.LoadNoteContent(card.Id);
		SelectedPriority = card.Priority;
		Tags = card.Tags;
		CreatedAt = card.CreatedAt;
		LastModified = card.LastModified;

		// Initialize history
		_editHistory.CreateRevision(NoteContent);
	}

	public int UndoCount => _editHistory.UndoCount;
	public int RedoCount => _editHistory.RedoCount;
	public bool WordWrapEnabled => IsWordWrapEnabled;

	[RelayCommand]
	private void GoBack() => _navigation.NavigateToBoard(_board);

	[RelayCommand]
	private void Save()
	{
		_dataService.SaveNoteContent(_card.Id, NoteContent);
		_card.Title = NoteTitle;
		_card.Priority = SelectedPriority;
		_card.Tags = Tags;
		_card.LastModified = DateTime.Now;
		_dataService.SaveBoard(_board);
		HasUnsavedChanges = false;
		StatusMessage = "Saved successfully";
	}

	[RelayCommand]
	private void Undo()
	{
		if (_editHistory.UndoCount > 0)
		{
			var result = _editHistory.Undo();
			if (result.HasValue)
			{
				NoteContent = result.Value.Content;
				// CursorPosition available at result.Value.CursorPosition if needed
			}
			StatusMessage = "Undo performed";
		}
	}

	[RelayCommand]
	private void Redo()
	{
		if (_editHistory.RedoCount > 0)
		{
			var result = _editHistory.Redo(NoteContent);
			if (result.HasValue)
			{
				NoteContent = result.Value.Content;
				// CursorPosition available at result.Value.CursorPosition if needed
			}
			StatusMessage = "Redo performed";
		}
	}

	[RelayCommand]
	private void ToggleWordWrap()
	{
		IsWordWrapEnabled = !IsWordWrapEnabled;
		StatusMessage = IsWordWrapEnabled ? "Word wrap enabled" : "Word wrap disabled";
	}

	[RelayCommand]
	private void ToggleLineNumbers()
	{
		ShowLineNumbers = !ShowLineNumbers;
		StatusMessage = ShowLineNumbers ? "Line numbers shown" : "Line numbers hidden";
	}

	[RelayCommand]
	private void ShowFindReplacePanel()
	{
		// Implementation for showing a find/replace panel
		StatusMessage = "Find/Replace panel opened";
	}

	[RelayCommand]
	private void ReplaceAll()
	{
		if (string.IsNullOrEmpty(FindText))
		{
			StatusMessage = "Find text is empty";
			return;
		}

		int replaceCount = 0;
		if (!string.IsNullOrEmpty(FindText))
		{
			replaceCount = (NoteContent.Length - NoteContent.Replace(FindText, ReplaceText).Length) / FindText.Length;
			NoteContent = NoteContent.Replace(FindText, ReplaceText);
			HasUnsavedChanges = true;
		}

		FindMatchCount = 0;
		StatusMessage = $"Replaced {replaceCount} occurrences";
	}

	[RelayCommand]
	private void InsertBold() => AppendText("**bold text**");

	[RelayCommand]
	private void InsertItalic() => AppendText("*italic text*");

	[RelayCommand]
	private void InsertStrikethrough() => AppendText("~~strikethrough~~");

	[RelayCommand]
	private void InsertCode() => AppendText("`code`");

	[RelayCommand]
	private void InsertCodeBlock() => AppendText("\n```\ncode block\n```\n");

	[RelayCommand]
	private void InsertHeading1() => AppendText("\n# Heading 1");

	[RelayCommand]
	private void InsertHeading2() => AppendText("\n## Heading 2");

	[RelayCommand]
	private void InsertHeading3() => AppendText("\n### Heading 3");

	[RelayCommand]
	private void InsertLink() => AppendText("[link text](url)");

	[RelayCommand]
	private void InsertImage() => AppendText("![alt text](image-url)");

	[RelayCommand]
	private void InsertBulletList() => AppendText("\n- List item");

	[RelayCommand]
	private void InsertNumberedList() => AppendText("\n1. List item");

	[RelayCommand]
	private void InsertCheckbox() => AppendText("\n- [ ] Task item");

	[RelayCommand]
	private void InsertQuote() => AppendText("\n> Quote text");

	[RelayCommand]
	private void InsertTable() => AppendText("\n| Header 1 | Header 2 |\n| -------- | -------- |\n| Cell 1   | Cell 2   |");

	private void AppendText(string text)
	{
		NoteContent += text;
		HasUnsavedChanges = true;
	}

	partial void OnNoteContentChanged(string value)
	{
		HasUnsavedChanges = true;
	}
}