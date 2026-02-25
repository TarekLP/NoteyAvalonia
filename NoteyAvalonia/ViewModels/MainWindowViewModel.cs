using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NoteToolAvalonia.Models;
using NoteToolAvalonia.Services;

namespace NoteToolAvalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly NavigationService _navigation;
    public DataService DataService { get; }

    [ObservableProperty]
    private ViewModelBase? _currentView;

    [ObservableProperty]
    private string _title = "NoteTool";

    [ObservableProperty]
    private bool _isSidebarVisible = true;

    public MainWindowViewModel(NavigationService navigation, DataService dataService)
    {
        _navigation = navigation;
        DataService = dataService;
    }

    public void NavigateToWelcome()
    {
        CurrentView = new WelcomeViewModel(_navigation, DataService);
        Title = "NoteTool - Welcome";
    }

    public void NavigateToBoard(Board board)
    {
        CurrentView = new BoardViewModel(board, _navigation, DataService);
        Title = $"NoteTool - {board.Name}";
    }

    public void NavigateToNoteEditor(NoteCard card, Board board, BoardColumn column)
    {
        CurrentView = new NoteEditorViewModel(card, board, column, _navigation, DataService);
        Title = $"NoteTool - Editing: {card.Title}";
    }

    public void NavigateToSettings()
    {
        CurrentView = new SettingsViewModel(_navigation, DataService);
        Title = "NoteTool - Settings";
    }

    [RelayCommand]
    private void GoHome() => NavigateToWelcome();

    [RelayCommand]
    private void GoSettings() => NavigateToSettings();

    [RelayCommand]
    private void ToggleSidebar() => IsSidebarVisible = !IsSidebarVisible;
}
