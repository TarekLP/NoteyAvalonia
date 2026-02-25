using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NoteToolAvalonia.Models;
using NoteToolAvalonia.Services;

namespace NoteToolAvalonia.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly NavigationService _navigation;
    private readonly DataService _dataService;

    [ObservableProperty] private string _selectedTheme;
    [ObservableProperty] private string _accentColor;
    [ObservableProperty] private double _fontSize;
    [ObservableProperty] private string _selectedFont;
    [ObservableProperty] private bool _autoSave;
    [ObservableProperty] private int _autoSaveInterval;
    [ObservableProperty] private bool _confirmBeforeDelete;
    [ObservableProperty] private bool _showCompletedNotes;
    [ObservableProperty] private string _dataFolderPath;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public ObservableCollection<string> Themes { get; } = new() { "Dark", "Light" };
    public ObservableCollection<string> Fonts { get; } = new() { "Inter", "Segoe UI", "Arial", "Cascadia Code", "Consolas" };
    public ObservableCollection<string> AccentColors { get; } = new()
    {
        "#007acc", "#3498db", "#2ecc71", "#e74c3c", "#9b59b6",
        "#f39c12", "#1abc9c", "#e67e22", "#e91e63", "#00bcd4"
    };

    public SettingsViewModel(NavigationService navigation, DataService dataService)
    {
        _navigation = navigation;
        _dataService = dataService;
        var s = _dataService.LoadSettings();
        _selectedTheme = s.Theme;
        _accentColor = s.AccentColor;
        _fontSize = s.FontSize;
        _selectedFont = s.FontFamily;
        _autoSave = s.AutoSave;
        _autoSaveInterval = s.AutoSaveIntervalSeconds;
        _confirmBeforeDelete = s.ConfirmBeforeDelete;
        _showCompletedNotes = s.ShowCompletedNotes;
        _dataFolderPath = dataService.DataFolder;
    }

    [RelayCommand]
    private void SaveSettings()
    {
        _dataService.SaveSettings(new AppSettings
        {
            Theme = SelectedTheme, AccentColor = AccentColor,
            FontSize = FontSize, FontFamily = SelectedFont,
            AutoSave = AutoSave, AutoSaveIntervalSeconds = AutoSaveInterval,
            ConfirmBeforeDelete = ConfirmBeforeDelete,
            ShowCompletedNotes = ShowCompletedNotes
        });
        StatusMessage = "Settings saved successfully!";
    }

    [RelayCommand]
    private void ResetDefaults()
    {
        SelectedTheme = "Dark"; AccentColor = "#007acc";
        FontSize = 14; SelectedFont = "Inter";
        AutoSave = true; AutoSaveInterval = 30;
        ConfirmBeforeDelete = true; ShowCompletedNotes = true;
        StatusMessage = "Defaults restored. Click Save to apply.";
    }

    [RelayCommand]
    private void GoBack() => _navigation.NavigateToWelcome();
}
