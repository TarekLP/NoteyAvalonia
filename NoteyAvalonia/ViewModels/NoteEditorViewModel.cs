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
	private readonly EditHistoryService _editHistory;
	private System.Threading.CancellationTokenSource? _autoSaveCancellation;
	private int _autoSaveDelayMs;

	[ObservableProperty]
	private string _noteTitle;

	[ObservableProperty]
	private string _noteContent;

	[ObservableProperty]
	private int _wordCount;

	[ObservableProperty]
	private int _characterCount;

	[ObservableProperty]
	private int _lineCount;

	[ObservableProperty]
	private bool _hasUnsavedChanges;

	[ObservableProperty]
	private bool _isPreviewVisible = true;

	[ObservableProperty]
	private bool _isFullscreen;

	[ObservableProperty]
	private NotePriority _selectedPriority;

	[ObservableProperty]
	private string _tags;

	[ObservableProperty]
	private DateTime _createdAt;

	[ObservableProperty]
	private DateTime _lastModified;

	[ObservableProperty]
	private string _statusMessage = "";

	[ObservableProperty]
	private bool _showLineNumbers = false;

	[ObservableProperty]
	private bool _wordWrapEnabled = true;

	[ObservableProperty]
	private int _undoCount = 0;

	[ObservableProperty]
	private int _redoCount = 0;

	[ObservableProperty]
	private string _findText = "";

	[ObservableProperty]
	private string _replaceText = "";

	[ObservableProperty]
	private bool _showFindReplace = false;

	[ObservableProperty]
	private int _findMatchCount = 0;

	[ObservableProperty]
	private int _currentFindMatch = 0;

	public ObservableCollection<NotePriority> PriorityLevels { get; } = new(Enum.GetValues<NotePriority>());

	public NoteEditorViewModel(NoteCard card, Board board, BoardColumn column,
							   NavigationService navigation, DataService dataService)
	{
		_card = card;
		_board = board;
		_navigation = navigation;
		_dataService = dataService;
		_editHistory = new EditHistoryService(maxUndoSteps: 50, maxRevisions: 100);

		_noteTitle = card.Title;
		_noteContent = card.Content;
		_selectedPriority = card.Priority;
		_tags = card.Tags;
		_createdAt = card.CreatedAt;
		_lastModified = card.LastModified;

		// Load auto-save settings from AppSettings
		LoadAutoSaveSettings();

		UpdateStatistics();
	}

	/// <summary>
	/// Load auto-save delay from AppSettings.
	/// Converts minutes to milliseconds, with a minimum of 1000ms.
	/// </summary>
	private void LoadAutoSaveSettings()
	{
		try
		{
			var settings = _dataService.LoadSettings();
			if (settings.AutoSave && settings.AutoSaveInterval > 0)
			{
				// Convert minutes to milliseconds (minimum 1 second)
				_autoSaveDelayMs = Math.Max(1000, settings.AutoSaveInterval * 1000);
			}
			else
			{
				// Auto-save disabled
				_autoSaveDelayMs = int.MaxValue; // Effectively disable by setting to huge value
			}
		}
		catch
		{
			// Default to 2.5 seconds if there's an error loading settings
			_autoSaveDelayMs = 2500;
		}
	}

	partial void OnNoteContentChanged(string value)
	{
		HasUnsavedChanges = true;
		UpdateStatistics();
		ScheduleAutoSave();
		UpdateFindMatches();
	}

	partial void OnNoteTitleChanged(string value)
	{
		HasUnsavedChanges = true;
		ScheduleAutoSave();
	}

	partial void OnSelectedPriorityChanged(NotePriority value)
	{
		HasUnsavedChanges = true;
		ScheduleAutoSave();
	}

	partial void OnTagsChanged(string value)
	{
		HasUnsavedChanges = true;
		ScheduleAutoSave();
	}

	partial void OnFindTextChanged(string value)
	{
		UpdateFindMatches();
	}

	private void UpdateStatistics()
	{
		if (string.IsNullOrWhiteSpace(NoteContent))
		{
			WordCount = 0;
			CharacterCount = 0;
			LineCount = 0;
		}
		else
		{
			WordCount = NoteContent.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
			CharacterCount = NoteContent.Length;
			LineCount = NoteContent.Split('\n').Length;
		}
	}

	private void UpdateUndoRedoCount()
	{
		UndoCount = _editHistory.UndoCount;
		RedoCount = _editHistory.RedoCount;
	}

	private void ScheduleAutoSave()
	{
		// Cancel any pending auto-save
		_autoSaveCancellation?.Cancel();
		_autoSaveCancellation = new System.Threading.CancellationTokenSource();

		// Only schedule if auto-save is enabled (delay is not max value)
		if (_autoSaveDelayMs == int.MaxValue)
			return;

		// Schedule a save after the delay
		Task.Delay(_autoSaveDelayMs, _autoSaveCancellation.Token)
			.ContinueWith(_ =>
			{
				if (_autoSaveCancellation?.Token.IsCancellationRequested == false)
				{
					AutoSave();
				}
			});
	}

	private void AutoSave()
	{
		_card.Title = string.IsNullOrWhiteSpace(NoteTitle) ? "Untitled Note" : NoteTitle;
		_card.Content = NoteContent ?? string.Empty;
		_card.Priority = SelectedPriority;
		_card.Tags = Tags ?? string.Empty;
		_card.LastModified = DateTime.Now;
		LastModified = _card.LastModified;

		_dataService.SaveBoard(_board);
		_editHistory.Push(NoteContent, 0); // Track for undo history
		UpdateUndoRedoCount();

		HasUnsavedChanges = false;
		StatusMessage = "Saving...";

		Task.Delay(1500).ContinueWith(_ =>
		{
			Avalonia.Threading.Dispatcher.UIThread.Post(() => StatusMessage = "");
		});
	}

	[RelayCommand]
	private void Save()
	{
		_card.Title = string.IsNullOrWhiteSpace(NoteTitle) ? "Untitled Note" : NoteTitle;
		_card.Content = NoteContent ?? string.Empty;
		_card.Priority = SelectedPriority;
		_card.Tags = Tags ?? string.Empty;
		_card.LastModified = DateTime.Now;
		LastModified = _card.LastModified;

		_dataService.SaveBoard(_board);
		HasUnsavedChanges = false;
		StatusMessage = "Saved!";

		Task.Delay(2000).ContinueWith(_ =>
		{
			Avalonia.Threading.Dispatcher.UIThread.Post(() => StatusMessage = "");
		});
	}

	[RelayCommand]
	private void Undo()
	{
		var previousState = _editHistory.Undo();
		if (previousState.HasValue)
		{
			NoteContent = previousState.Value.Content;
			UpdateStatistics();
			UpdateUndoRedoCount();
			ShowStatusMessage("↶ Undo");
		}
	}

	[RelayCommand]
	private void Redo()
	{
		var nextState = _editHistory.Redo();
		if (nextState.HasValue)
		{
			NoteContent = nextState.Value.Content;
			UpdateStatistics();
			UpdateUndoRedoCount();
			ShowStatusMessage("↷ Redo");
		}
	}

	private void ShowStatusMessage(string message)
	{
		StatusMessage = message;
		Task.Delay(800).ContinueWith(_ =>
		{
			Avalonia.Threading.Dispatcher.UIThread.Post(() => StatusMessage = "");
		});
	}

	[RelayCommand]
	private void GoBack()
	{
		if (HasUnsavedChanges) Save();
		_navigation.NavigateToBoard(_board);
	}

	[RelayCommand]
	private void Delete()
	{
		foreach (var col in _board.Columns)
		{
			var toRemove = col.Cards.FirstOrDefault(c => c.Id == _card.Id);
			if (toRemove != null) { col.Cards.Remove(toRemove); break; }
		}
		_dataService.SaveBoard(_board);
		_navigation.NavigateToBoard(_board);
	}

	[RelayCommand]
	private void TogglePreview()
	{
		IsPreviewVisible = !IsPreviewVisible;
	}

	[RelayCommand]
	private void ToggleFullscreen()
	{
		IsFullscreen = !IsFullscreen;
	}

	[RelayCommand]
	private void ToggleLineNumbers()
	{
		ShowLineNumbers = !ShowLineNumbers;
	}

	[RelayCommand]
	private void ToggleWordWrap()
	{
		WordWrapEnabled = !WordWrapEnabled;
	}

	[RelayCommand]
	private void ShowFindReplacePanel()
	{
		ShowFindReplace = !ShowFindReplace;
		if (!ShowFindReplace)
		{
			FindText = "";
			ReplaceText = "";
		}
	}

	private void UpdateFindMatches()
	{
		if (string.IsNullOrWhiteSpace(FindText) || string.IsNullOrWhiteSpace(NoteContent))
		{
			FindMatchCount = 0;
			CurrentFindMatch = 0;
			return;
		}

		try
		{
			var matches = Regex.Matches(NoteContent, Regex.Escape(FindText), RegexOptions.IgnoreCase);
			FindMatchCount = matches.Count;
			CurrentFindMatch = FindMatchCount > 0 ? 1 : 0;
		}
		catch
		{
			FindMatchCount = 0;
			CurrentFindMatch = 0;
		}
	}

	[RelayCommand]
	private void ReplaceAll()
	{
		if (string.IsNullOrWhiteSpace(FindText) || string.IsNullOrWhiteSpace(NoteContent))
			return;

		try
		{
			var newContent = Regex.Replace(NoteContent, Regex.Escape(FindText), ReplaceText, RegexOptions.IgnoreCase);
			NoteContent = newContent;
			UpdateFindMatches();
			ShowStatusMessage($"Replaced {FindMatchCount} occurrences");
		}
		catch
		{
			ShowStatusMessage("Replace failed");
		}
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

	[RelayCommand]
	private void InsertHorizontalRule() => AppendText("\n---\n");

	private void AppendText(string text)
	{
		NoteContent = (NoteContent ?? "") + text;
	}
}
