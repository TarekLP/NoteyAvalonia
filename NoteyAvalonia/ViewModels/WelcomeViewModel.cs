using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NoteToolAvalonia.Models;
using NoteToolAvalonia.Services;

namespace NoteToolAvalonia.ViewModels;

public partial class WelcomeViewModel : ViewModelBase
{
    private readonly NoteyService _service;
    private List<NoteCard> _allNotes = new();

    [ObservableProperty] private ObservableCollection<NoteCard> _filteredNotes = new();

    // ── Filters ────────────────────────────────────────────
    [ObservableProperty] private string _searchText             = string.Empty;
    [ObservableProperty] private string _selectedCategory       = "All";
    [ObservableProperty] private string _selectedDeadlineFilter = "All";
    [ObservableProperty] private string _selectedSort           = "Newest";
    [ObservableProperty] private bool   _showCompleted          = true;

    // ── Creator overlay ────────────────────────────────────
    [ObservableProperty] private bool   _isCreatingNote  = false;
    [ObservableProperty] private string _newNoteTitle    = string.Empty;
    [ObservableProperty] private string _newNoteCategory = string.Empty;
    [ObservableProperty] private string _newNoteTags     = string.Empty;
    [ObservableProperty] private string _newNoteDeadline = string.Empty;
    [ObservableProperty] private int    _creatorStep     = 1;

    // ── Filter lists ───────────────────────────────────────
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
                     .Distinct().OrderBy(c => c))
            Categories.Add(cat);
        SelectedCategory = Categories.Contains(current) ? current : "All";
    }

    // ══════════════════════════════════════════════════════
    //  FILTERS
    // ══════════════════════════════════════════════════════

    partial void OnSearchTextChanged(string _)             => ApplyFilters();
    partial void OnSelectedCategoryChanged(string _)       => ApplyFilters();
    partial void OnSelectedDeadlineFilterChanged(string _) => ApplyFilters();
    partial void OnSelectedSortChanged(string _)           => ApplyFilters();
    partial void OnShowCompletedChanged(bool _)            => ApplyFilters();

    private void ApplyFilters()
    {
        var result = _allNotes.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
            result = result.Where(n =>
                n.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                n.Category.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                n.Tags.Any(t => t.Contains(SearchText, StringComparison.OrdinalIgnoreCase)));

        if (SelectedCategory != "All")
            result = result.Where(n => n.Category == SelectedCategory);

        if (!ShowCompleted)
            result = result.Where(n => !n.IsCompleted);

        result = SelectedDeadlineFilter switch
        {
            "Overdue"     => result.Where(n => n.Deadline.HasValue && n.Deadline.Value.Date < DateTime.Today),
            "This Week"   => result.Where(n => n.Deadline.HasValue &&
                                               n.Deadline.Value.Date >= DateTime.Today &&
                                               n.Deadline.Value.Date <= DateTime.Today.AddDays(7)),
            "No Deadline" => result.Where(n => !n.Deadline.HasValue),
            _             => result
        };

        result = SelectedSort switch
        {
            "Oldest"   => result.OrderBy(n => n.CreatedAt),
            "A → Z"    => result.OrderBy(n => n.Title, StringComparer.OrdinalIgnoreCase),
            "Z → A"    => result.OrderByDescending(n => n.Title, StringComparer.OrdinalIgnoreCase),
            "Deadline" => result.OrderBy(n => n.Deadline ?? DateTime.MaxValue),
            _          => result.OrderByDescending(n => n.LastModified)
        };

        FilteredNotes = new ObservableCollection<NoteCard>(result);
    }

    // ══════════════════════════════════════════════════════
    //  NOTE ACTIONS
    // ══════════════════════════════════════════════════════

    [RelayCommand]
    private void OpenNote(NoteCard card) =>
        _service.NavigateToNoteEditor(card);   // via NoteyService, never via ApplicationLifetime

    [RelayCommand]
    private void DeleteNote(NoteCard card)
    {
        _service.DeleteNote(card.Id);
        LoadNotes();
    }

    // ══════════════════════════════════════════════════════
    //  NOTE CREATOR
    // ══════════════════════════════════════════════════════

    [RelayCommand]
    private void ShowCreateNote()
    {
        NewNoteTitle = NewNoteCategory = NewNoteTags = NewNoteDeadline = string.Empty;
        CreatorStep    = 1;
        IsCreatingNote = true;
    }

    [RelayCommand] private void CreatorNext() { if (CreatorStep < 4) CreatorStep++; }
    [RelayCommand] private void CreatorBack() { if (CreatorStep > 1) CreatorStep--; }
    [RelayCommand] private void CancelCreateNote() => IsCreatingNote = false;

    [RelayCommand]
    private void CreateNote()
    {
        if (string.IsNullOrWhiteSpace(NewNoteTitle)) return;

        DateTime? deadline = DateTime.TryParse(NewNoteDeadline, out var d) ? d : null;

        var card = new NoteCard
        {
            Title    = NewNoteTitle.Trim(),
            Category = NewNoteCategory.Trim(),
            Tags     = NewNoteTags
                           .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                           .ToList(),
            Deadline = deadline
        };

        _service.SaveNote(card, string.Empty);
        IsCreatingNote = false;
        LoadNotes();

        _service.NavigateToNoteEditor(card);
    }
}
