using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NoteToolAvalonia.Models;
using NoteToolAvalonia.Services;

namespace NoteToolAvalonia.ViewModels;

public partial class WelcomeViewModel : ViewModelBase
{
    private readonly NoteyService _service;
    private List<NoteCard> _allNotes = new();

    // ── All notes loaded from disk ─────────────────────────
    [ObservableProperty] private ObservableCollection<NoteCard> _filteredNotes = new();

    // ── Filter controls ────────────────────────────────────
    [ObservableProperty] private string _searchText      = string.Empty;
    [ObservableProperty] private string _selectedCategory = "All";
    [ObservableProperty] private string _selectedDeadlineFilter = "All";
    [ObservableProperty] private string _selectedSort    = "Newest";
    [ObservableProperty] private bool   _showCompleted   = true;

    // ── New note creator overlay ───────────────────────────
    [ObservableProperty] private bool   _isCreatingNote  = false;
    [ObservableProperty] private string _newNoteTitle    = string.Empty;
    [ObservableProperty] private string _newNoteCategory = string.Empty;
    [ObservableProperty] private string _newNoteTags     = string.Empty;
    [ObservableProperty] private string _newNoteDeadline = string.Empty;
    [ObservableProperty] private int    _creatorStep     = 1;

    // ── Filter option lists ────────────────────────────────
    public ObservableCollection<string> Categories      { get; } = new() { "All" };
    public List<string> DeadlineFilters { get; } = new() { "All", "Overdue", "This Week", "No Deadline" };
    public List<string> SortOptions     { get; } = new() { "Newest", "Oldest", "A → Z", "Z → A", "Deadline" };

    // ── Stats ──────────────────────────────────────────────
    public int TotalNotes     => _allNotes.Count;
    public int CompletedNotes => _allNotes.Count(n => n.IsCompleted);
    public int OverdueNotes   => _allNotes.Count(n =>
        n.Deadline.HasValue && n.Deadline.Value.Date < DateTime.Today && !n.IsCompleted);

    public WelcomeViewModel(NoteyService service)
    {
        _service = service;
        LoadNotes();
    }

    // ══════════════════════════════════════════════════════
    //  LOADING
    // ══════════════════════════════════════════════════════

    private void LoadNotes()
    {
        _allNotes = _service.LoadNotesIndex();
        RebuildCategoryList();
        ApplyFilters();
        OnPropertyChanged(nameof(TotalNotes));
        OnPropertyChanged(nameof(CompletedNotes));
        OnPropertyChanged(nameof(OverdueNotes));
    }

    private void RebuildCategoryList()
    {
        var current = SelectedCategory;
        Categories.Clear();
        Categories.Add("All");
        foreach (var cat in _allNotes
                     .Select(n => n.Category)
                     .Where(c => !string.IsNullOrWhiteSpace(c))
                     .Distinct()
                     .OrderBy(c => c))
        {
            Categories.Add(cat);
        }
        // Restore selection if it still exists, else fall back to All
        SelectedCategory = Categories.Contains(current) ? current : "All";
    }

    // ══════════════════════════════════════════════════════
    //  FILTERING & SORTING
    // ══════════════════════════════════════════════════════

    partial void OnSearchTextChanged(string _)           => ApplyFilters();
    partial void OnSelectedCategoryChanged(string _)     => ApplyFilters();
    partial void OnSelectedDeadlineFilterChanged(string _) => ApplyFilters();
    partial void OnSelectedSortChanged(string _)         => ApplyFilters();
    partial void OnShowCompletedChanged(bool _)          => ApplyFilters();

    private void ApplyFilters()
    {
        var result = _allNotes.AsEnumerable();

        // Search
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var q = SearchText.Trim().ToLowerInvariant();
            result = result.Where(n =>
                n.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                n.Category.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                n.Tags.Any(t => t.Contains(q, StringComparison.OrdinalIgnoreCase)));
        }

        // Category
        if (SelectedCategory != "All")
            result = result.Where(n => n.Category == SelectedCategory);

        // Completed
        if (!ShowCompleted)
            result = result.Where(n => !n.IsCompleted);

        // Deadline
        result = SelectedDeadlineFilter switch
        {
            "Overdue"     => result.Where(n => n.Deadline.HasValue && n.Deadline.Value.Date < DateTime.Today),
            "This Week"   => result.Where(n => n.Deadline.HasValue &&
                                               n.Deadline.Value.Date >= DateTime.Today &&
                                               n.Deadline.Value.Date <= DateTime.Today.AddDays(7)),
            "No Deadline" => result.Where(n => !n.Deadline.HasValue),
            _             => result
        };

        // Sort
        result = SelectedSort switch
        {
            "Oldest"    => result.OrderBy(n => n.CreatedAt),
            "A → Z"     => result.OrderBy(n => n.Title, StringComparer.OrdinalIgnoreCase),
            "Z → A"     => result.OrderByDescending(n => n.Title, StringComparer.OrdinalIgnoreCase),
            "Deadline"  => result.OrderBy(n => n.Deadline ?? DateTime.MaxValue),
            _           => result.OrderByDescending(n => n.LastModified)   // Newest
        };

        FilteredNotes = new ObservableCollection<NoteCard>(result);
    }

    // ══════════════════════════════════════════════════════
    //  NOTE ACTIONS
    // ══════════════════════════════════════════════════════

    [RelayCommand]
    private void OpenNote(NoteCard card)
    {
        if (Application.Current?.ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime { MainWindow.DataContext: MainWindowViewModel mainVm })
        {
            mainVm.NavigateToNoteEditor(card);
        }
    }

    [RelayCommand]
    private void DeleteNote(NoteCard card)
    {
        _service.DeleteNote(card.Id);
        LoadNotes();
    }

    [RelayCommand]
    private void TogglePin(NoteCard card)
    {
        card.IsPinned = !card.IsPinned;
        _service.SaveNote(card, _service.LoadNoteContent(card.Id));
        LoadNotes();
    }

    // ══════════════════════════════════════════════════════
    //  NOTE CREATOR  (multi-step overlay)
    // ══════════════════════════════════════════════════════

    [RelayCommand]
    private void ShowCreateNote()
    {
        NewNoteTitle    = string.Empty;
        NewNoteCategory = string.Empty;
        NewNoteTags     = string.Empty;
        NewNoteDeadline = string.Empty;
        CreatorStep     = 1;
        IsCreatingNote  = true;
    }

    [RelayCommand]
    private void CreatorNext()
    {
        if (CreatorStep < 4) CreatorStep++;
    }

    [RelayCommand]
    private void CreatorBack()
    {
        if (CreatorStep > 1) CreatorStep--;
    }

    [RelayCommand]
    private void CancelCreateNote()
    {
        IsCreatingNote = false;
    }

    [RelayCommand]
    private void CreateNote()
    {
        if (string.IsNullOrWhiteSpace(NewNoteTitle)) return;

        var tags = NewNoteTags
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        DateTime? deadline = null;
        if (DateTime.TryParse(NewNoteDeadline, out var d)) deadline = d;

        var card = new NoteCard
        {
            Title    = NewNoteTitle.Trim(),
            Category = NewNoteCategory.Trim(),
            Tags     = tags,
            Deadline = deadline
        };

        // Creates the .md file with frontmatter, empty body
        _service.SaveNote(card, string.Empty);

        IsCreatingNote = false;
        LoadNotes();

        // Navigate straight into the editor
        OpenNote(card);
    }
}
