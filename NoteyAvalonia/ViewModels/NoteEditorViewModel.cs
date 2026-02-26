using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NoteToolAvalonia.Models;
using NoteToolAvalonia.Services;

namespace NoteToolAvalonia.ViewModels;

public partial class NoteEditorViewModel : ViewModelBase
{
    private readonly NavigationService _navigation;
    private readonly DataService _dataService;
    private readonly Board _board;
    private readonly BoardColumn _column;
    private readonly NoteCard _card;

    [ObservableProperty] private string _noteTitle;
    [ObservableProperty] private string _noteContent;
    [ObservableProperty] private NotePriority _notePriority;
    [ObservableProperty] private string _noteTags;
    [ObservableProperty] private bool _isCompleted;
    [ObservableProperty] private DateTime _createdAt;
    [ObservableProperty] private DateTime _lastModified;
    [ObservableProperty] private string _columnName;
    [ObservableProperty] private int _wordCount;
    [ObservableProperty] private int _charCount;
    [ObservableProperty] private bool _hasUnsavedChanges;

    public NotePriority[] PriorityValues => Enum.GetValues<NotePriority>();

    public NoteEditorViewModel(NoteCard card, Board board, BoardColumn column,
                               NavigationService navigation, DataService dataService)
    {
        _card = card;
        _board = board;
        _column = column;
        _navigation = navigation;
        _dataService = dataService;
        _noteTitle = card.Title;
        _noteContent = card.Content;
        _notePriority = card.Priority;
        _noteTags = card.Tags;
        _isCompleted = card.IsCompleted;
        _createdAt = card.CreatedAt;
        _lastModified = card.LastModified;
        _columnName = column.Title;
        UpdateCounts();
    }

    partial void OnNoteContentChanged(string value) { HasUnsavedChanges = true; UpdateCounts(); }
    partial void OnNoteTitleChanged(string value) => HasUnsavedChanges = true;
    partial void OnNotePriorityChanged(NotePriority value) => HasUnsavedChanges = true;
    partial void OnNoteTagsChanged(string value) => HasUnsavedChanges = true;
    partial void OnIsCompletedChanged(bool value) => HasUnsavedChanges = true;

    private void UpdateCounts()
    {
        CharCount = NoteContent?.Length ?? 0;
        WordCount = string.IsNullOrWhiteSpace(NoteContent) ? 0
            : NoteContent.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    [RelayCommand]
    private void Save()
    {
        _card.Title = NoteTitle;
        _card.Content = NoteContent;
        _card.Priority = NotePriority;
        _card.Tags = NoteTags;
        _card.IsCompleted = IsCompleted;
        _card.LastModified = DateTime.Now;
        LastModified = _card.LastModified;
        foreach (var col in _board.Columns)
        {
            var existing = col.Cards.FirstOrDefault(c => c.Id == _card.Id);
            if (existing != null)
            {
                col.Cards[col.Cards.IndexOf(existing)] = _card;
                break;
            }
        }
        _board.LastModified = DateTime.Now;
        _dataService.SaveBoard(_board);
        HasUnsavedChanges = false;
    }

    [RelayCommand]
    private void SaveAndGoBack() { Save(); _navigation.NavigateToBoard(_board); }

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
			if (toRemove != null)
			{
				col.Cards.Remove(toRemove);
				break;
			}
		}
		_board.LastModified = DateTime.Now;
        _dataService.SaveBoard(_board);
        _navigation.NavigateToBoard(_board);
    }

    [RelayCommand]
    private void InsertTimestamp()
    {
        NoteContent = (NoteContent ?? "") + $"\n[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ";
    }

    [RelayCommand]
    private void InsertCheckbox() { NoteContent = (NoteContent ?? "") + "\nâ˜ "; }

    [RelayCommand]
    private void InsertSeparator() { NoteContent = (NoteContent ?? "") + "\nâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€\n"; }
}
