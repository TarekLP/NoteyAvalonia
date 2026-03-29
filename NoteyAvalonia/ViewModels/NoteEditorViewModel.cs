using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NoteToolAvalonia.Models;
using NoteToolAvalonia.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace NoteToolAvalonia.ViewModels;

public partial class NoteEditorViewModel : ViewModelBase
{
	private readonly NavigationService _navigation;
	private readonly DataService _dataService;
	private readonly Board _board;
	private readonly NoteCard _card;

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

	public ObservableCollection<NotePriority> PriorityLevels { get; } = new(Enum.GetValues<NotePriority>());

	public NoteEditorViewModel(NoteCard card, Board board, BoardColumn column,
							   NavigationService navigation, DataService dataService)
	{
		_card = card;
		_board = board;
		_navigation = navigation;
		_dataService = dataService;

		_noteTitle = card.Title;
		_noteContent = card.Content;
		_selectedPriority = card.Priority;
		_tags = card.Tags;
		_createdAt = card.CreatedAt;
		_lastModified = card.LastModified;

		UpdateStatistics();
	}

	partial void OnNoteContentChanged(string value)
	{
		HasUnsavedChanges = true;
		UpdateStatistics();
	}

	partial void OnNoteTitleChanged(string value)
	{
		HasUnsavedChanges = true;
	}

	partial void OnSelectedPriorityChanged(NotePriority value)
	{
		HasUnsavedChanges = true;
	}

	partial void OnTagsChanged(string value)
	{
		HasUnsavedChanges = true;
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

		System.Threading.Tasks.Task.Delay(2000).ContinueWith(_ =>
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