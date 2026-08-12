using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NoteToolAvalonia.Models;
using NoteToolAvalonia.Services;
using NoteToolAvalonia.Views;

namespace NoteToolAvalonia.ViewModels;

public partial class NoteEditorViewModel : ViewModelBase
{
    private readonly NoteyService _service;
    private readonly NoteCard     _card;
    private CancellationTokenSource? _autoSaveCts;
    private int _autoSaveDelayMs;

    // ── Document ───────────────────────────────────────────
    [ObservableProperty] private string _noteTitle;
    [ObservableProperty] private string _noteContent;

    // ── Metadata (topbar) ──────────────────────────────────
    [ObservableProperty] private string  _category;
    [ObservableProperty] private string  _tagsDisplay;        // comma-separated for editing
    [ObservableProperty] private string  _deadlineText;       // human-readable
    [ObservableProperty] private bool    _hasDeadline;
	[ObservableProperty] private DateTime? _deadline;
	[ObservableProperty] private bool    _isOverdue;

    // ── References panel ───────────────────────────────────
    [ObservableProperty] private bool _isReferencesPanelOpen;
    [ObservableProperty] private ObservableCollection<NoteCard> _referencedNotes = new();
    [ObservableProperty] private ObservableCollection<NoteCard> _allOtherNotes   = new();
    [ObservableProperty] private string _referenceSearch = string.Empty;

    // ── Markdown preview pane ──────────────────────────────
    [ObservableProperty] private bool _isPreviewVisible;

    // ── Completion / pinning ───────────────────────────────
    [ObservableProperty] private bool _isCompleted;
    [ObservableProperty] private bool _isPinned;

    // ── Editor state ──────────────────────────────────────────
    [ObservableProperty] private bool   _hasUnsavedChanges;
    [ObservableProperty] private bool   _showLineNumbers  = false;
    [ObservableProperty] private bool   _wordWrapEnabled  = true;
    [ObservableProperty] private bool   _showFindReplace  = false;
    [ObservableProperty] private string _findText         = string.Empty;
    [ObservableProperty] private string _replaceText      = string.Empty;
    [ObservableProperty] private int    _findMatchCount;
    [ObservableProperty] private int    _wordCount;
    [ObservableProperty] private int    _characterCount;
    [ObservableProperty] private int    _lineCount;
    [ObservableProperty] private string _lineNumbersText = string.Empty;
    [ObservableProperty] private string _statusMessage    = string.Empty;
    [ObservableProperty] private int    _undoCount;
    [ObservableProperty] private int    _redoCount;

    public ObservableCollection<NotePriority> PriorityLevels { get; } =
        new(Enum.GetValues<NotePriority>());

    [ObservableProperty] private NotePriority _selectedPriority;

    // ══════════════════════════════════════════════════════
    //  INIT
    // ══════════════════════════════════════════════════════

    public NoteEditorViewModel(NoteCard card, NoteyService service)
    {
        _card    = card;
        _service = service;

        _noteTitle   = card.Title;
        _noteContent = _service.LoadNoteContent(card.Id);
        _category    = card.Category;
        _tagsDisplay = string.Join(", ", card.Tags);
        _selectedPriority = card.Priority;
        _isCompleted = card.IsCompleted;
        _isPinned    = card.IsPinned;
        _deadlineText = string.Empty;

        if (card.Deadline.HasValue)
        {
            _hasDeadline  = true;
            _deadlineText = card.Deadline.Value.ToString("MMM dd, yyyy");
            _isOverdue    = card.Deadline.Value.Date < DateTime.Today;
        }

        _service.History.Clear();
        _service.History.Snapshot(_noteContent);

        LoadAutoSaveSettings();
        UpdateStatistics();
        LoadReferencedNotes();
        IsPreviewVisible = _service.LoadSettings().ShowPreview;
    }

    private void LoadAutoSaveSettings()
    {
        try
        {
            var s = _service.LoadSettings();
            _autoSaveDelayMs = s.AutoSave && s.AutoSaveInterval > 0
                ? Math.Max(1000, s.AutoSaveInterval * 1000)
                : int.MaxValue;
        }
        catch { _autoSaveDelayMs = 2500; }
    }

    // ══════════════════════════════════════════════════════
    //  PROPERTY CHANGE HOOKS
    // ══════════════════════════════════════════════════════

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

    partial void OnCategoryChanged(string value)
    {
        HasUnsavedChanges = true;
        ScheduleAutoSave();
    }

    partial void OnTagsDisplayChanged(string value)
    {
        HasUnsavedChanges = true;
        ScheduleAutoSave();
    }

    partial void OnIsCompletedChanged(bool value)
    {
        HasUnsavedChanges = true;
        ScheduleAutoSave();
    }

    partial void OnIsPinnedChanged(bool value)
    {
        HasUnsavedChanges = true;
        ScheduleAutoSave();
    }

    partial void OnFindTextChanged(string value) => UpdateFindMatches();

    partial void OnReferenceSearchChanged(string value) => FilterReferenceSearch(value);

    // ══════════════════════════════════════════════════════
    //  SAVE
    // ══════════════════════════════════════════════════════

    private void FlushToCard()
    {
        _card.Title    = string.IsNullOrWhiteSpace(NoteTitle) ? "Untitled Note" : NoteTitle.Trim();
        _card.Category = Category.Trim();
        _card.Priority = SelectedPriority;
        _card.Tags     = TagsDisplay
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        _card.IsCompleted = IsCompleted;
        _card.IsPinned    = IsPinned;
    }

    [RelayCommand]
    private void Save()
    {
        FlushToCard();
        _service.SaveNote(_card, NoteContent ?? string.Empty);
        HasUnsavedChanges = false;
        ShowStatus("Saved!");
    }

    private void AutoSave()
    {
        FlushToCard();
        _service.History.Push(NoteContent, 0);
        _service.SaveNote(_card, NoteContent ?? string.Empty);
        UndoCount = _service.History.UndoCount;
        RedoCount = _service.History.RedoCount;
        HasUnsavedChanges = false;
        ShowStatus("Auto-saved");
    }

    private void ScheduleAutoSave()
    {
        _autoSaveCts?.Cancel();
        _autoSaveCts = new CancellationTokenSource();
        if (_autoSaveDelayMs == int.MaxValue) return;

        var token = _autoSaveCts.Token;
        Task.Delay(_autoSaveDelayMs, token).ContinueWith(_ =>
        {
            if (!token.IsCancellationRequested)
                Avalonia.Threading.Dispatcher.UIThread.Post(AutoSave);
        }, token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
    }

    // ══════════════════════════════════════════════════════
    //  NAVIGATION
    // ══════════════════════════════════════════════════════

    [RelayCommand]
    private void GoBack()
    {
        if (HasUnsavedChanges) Save();
        _service.NavigateToWelcome();
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (_service.LoadSettings().ConfirmBeforeDelete &&
            !await Dialogs.ConfirmAsync("Delete note",
                $"Delete \"{_card.Title}\" permanently? This cannot be undone."))
            return;

        _service.DeleteNote(_card.Id);
        _service.NavigateToWelcome();
    }

    [RelayCommand]
    private async Task Export()
    {
        if (Application.Current?.ApplicationLifetime
            is not IClassicDesktopStyleApplicationLifetime { MainWindow: { } window }) return;

        var topLevel = TopLevel.GetTopLevel(window);
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "Export note as HTML",
                SuggestedFileName = $"{_card.Title}.html",
                DefaultExtension = "html",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("HTML document") { Patterns = new[] { "*.html" } }
                }
            });

        if (file == null) return;

        if (HasUnsavedChanges) Save();
        _service.ExportNote(_card.Id, file.Path.LocalPath);
        ShowStatus("Exported as HTML");
    }

    // ══════════════════════════════════════════════════════
    //  UNDO / REDO
    // ══════════════════════════════════════════════════════

    [RelayCommand]
    private void Undo()
    {
        var state = _service.History.Undo();
        if (state is null) return;
        NoteContent = state.Value.Content;
        UndoCount   = _service.History.UndoCount;
        RedoCount   = _service.History.RedoCount;
        ShowStatus("↶ Undo");
    }

    [RelayCommand]
    private void Redo()
    {
        var state = _service.History.Redo();
        if (state is null) return;
        NoteContent = state.Value.Content;
        UndoCount   = _service.History.UndoCount;
        RedoCount   = _service.History.RedoCount;
        ShowStatus("↷ Redo");
    }

    // ══════════════════════════════════════════════════════
    //  DEADLINE
    // ══════════════════════════════════════════════════════

    [RelayCommand]
    private void SetDeadline(string? dateStr)
    {
        if (DateTime.TryParse(dateStr, out var d))
        {
            _card.Deadline = d;
            HasDeadline    = true;
            DeadlineText   = d.ToString("MMM dd, yyyy");
            IsOverdue      = d.Date < DateTime.Today;
        }
        HasUnsavedChanges = true;
    }

    [RelayCommand]
    private void ClearDeadline()
    {
        _card.Deadline = null;
        HasDeadline    = false;
        DeadlineText   = string.Empty;
        IsOverdue      = false;
        HasUnsavedChanges = true;
    }

    // ══════════════════════════════════════════════════════
    //  REFERENCES PANEL
    // ══════════════════════════════════════════════════════

    partial void OnIsPreviewVisibleChanged(bool value)
    {
        try
        {
            var s = _service.LoadSettings();
            s.ShowPreview = value;
            _service.SaveSettings(s);
        }
        catch { /* ponytail: settings persistence is best-effort */ }
    }

    [RelayCommand]
    private void TogglePreview() => IsPreviewVisible = !IsPreviewVisible;

    [RelayCommand]
    private void ToggleReferencesPanel()
    {
        IsReferencesPanelOpen = !IsReferencesPanelOpen;
        if (IsReferencesPanelOpen) LoadAllOtherNotes();
    }

    [RelayCommand]
    private void OpenReferencedNote(NoteCard card)
    {
        if (HasUnsavedChanges) Save();
        _service.NavigateToNoteEditor(card);
    }

    [RelayCommand]
    private void AddReference(NoteCard card)
    {
        if (_card.References.Contains(card.Id)) return;
        _card.References.Add(card.Id);
        HasUnsavedChanges = true;
        LoadReferencedNotes();
    }

    [RelayCommand]
    private void RemoveReference(NoteCard card)
    {
        _card.References.Remove(card.Id);
        HasUnsavedChanges = true;
        LoadReferencedNotes();
    }

    private void LoadReferencedNotes()
    {
        ReferencedNotes.Clear();
        var index = _service.LoadNotesIndex();
        foreach (var id in _card.References)
        {
            var note = index.FirstOrDefault(n => n.Id == id);
            if (note != null) ReferencedNotes.Add(note);
        }
    }

    private void LoadAllOtherNotes()
    {
        AllOtherNotes.Clear();
        var index = _service.LoadNotesIndex();
        foreach (var note in index.Where(n => n.Id != _card.Id))
            AllOtherNotes.Add(note);
    }

    private void FilterReferenceSearch(string query)
    {
        var index = _service.LoadNotesIndex();
        AllOtherNotes.Clear();
        var results = string.IsNullOrWhiteSpace(query)
            ? index.Where(n => n.Id != _card.Id)
            : index.Where(n => n.Id != _card.Id &&
                               n.Title.Contains(query, StringComparison.OrdinalIgnoreCase));
        foreach (var n in results) AllOtherNotes.Add(n);
    }

    // ══════════════════════════════════════════════════════
    //  FIND & REPLACE
    // ══════════════════════════════════════════════════════

    [RelayCommand]
    private void ShowFindReplacePanel()
    {
        ShowFindReplace = !ShowFindReplace;
        if (!ShowFindReplace) { FindText = string.Empty; ReplaceText = string.Empty; }
    }

    private void UpdateFindMatches()
    {
        if (string.IsNullOrWhiteSpace(FindText) || string.IsNullOrWhiteSpace(NoteContent))
        { FindMatchCount = 0; return; }
        try { FindMatchCount = Regex.Matches(NoteContent, Regex.Escape(FindText), RegexOptions.IgnoreCase).Count; }
        catch { FindMatchCount = 0; }
    }

    [RelayCommand]
    private void ReplaceAll()
    {
        if (string.IsNullOrWhiteSpace(FindText)) return;
        try
        {
            NoteContent = Regex.Replace(NoteContent, Regex.Escape(FindText), ReplaceText ?? string.Empty, RegexOptions.IgnoreCase);
            ShowStatus($"Replaced {FindMatchCount} occurrences");
        }
        catch { ShowStatus("Replace failed"); }
    }

    // ══════════════════════════════════════════════════════
    //  TOOLBAR TOGGLES
    // ══════════════════════════════════════════════════════

    [RelayCommand] private void ToggleLineNumbers() => ShowLineNumbers = !ShowLineNumbers;
    [RelayCommand] private void ToggleWordWrap()    => WordWrapEnabled  = !WordWrapEnabled;

    // ══════════════════════════════════════════════════════
    //  HELPERS
    // ══════════════════════════════════════════════════════

    private void UpdateStatistics()
    {
        if (string.IsNullOrWhiteSpace(NoteContent))
        { WordCount = 0; CharacterCount = 0; LineCount = 0; return; }

        WordCount      = NoteContent.Split(new[] { ' ', '\n', '\r', '\t' },
                             StringSplitOptions.RemoveEmptyEntries).Length;
        CharacterCount = NoteContent.Length;
        LineCount      = NoteContent.Split('\n').Length;
        LineNumbersText = string.Join("\n", Enumerable.Range(1, LineCount));
    }

    private void ShowStatus(string msg)
    {
        StatusMessage = msg;
        Task.Delay(2000).ContinueWith(_ =>
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (StatusMessage == msg) StatusMessage = string.Empty;
            }));
    }
}
