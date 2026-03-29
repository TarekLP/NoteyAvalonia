using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NoteToolAvalonia.Models;
using NoteToolAvalonia.Services;

namespace NoteToolAvalonia.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    public DataService DataService { get; }

    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private string _selectedTheme;
    [ObservableProperty] private string _selectedFont;
    [ObservableProperty] private double _fontSize;
    [ObservableProperty] private bool _autoSave;
    [ObservableProperty] private int _autoSaveInterval;
    [ObservableProperty] private bool _confirmBeforeDelete;
    [ObservableProperty] private bool _showCompletedNotes;
    [ObservableProperty] private string _dataFolderPath;

    public List<string> Themes { get; } = new() { "Dark", "Light", "System" };
    public List<string> Fonts { get; } = new() { "Inter", "Roboto", "Segoe UI", "Arial" };

    public ICommand SaveSettingsCommand { get; }
    public ICommand ResetDefaultsCommand { get; }
    public ICommand GoBackCommand { get; }

    public SettingsViewModel(DataService? dataService)
    {
        DataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
        _dataFolderPath = DataService.DataFolder;

        // Load existing settings
        var settings = DataService.LoadSettings();
        LoadFromModel(settings);

        SaveSettingsCommand = new RelayCommand(SaveSettings);
        ResetDefaultsCommand = new RelayCommand(ResetToDefaults);
        GoBackCommand = new RelayCommand(() => { /* Add navigation logic here */ });
    }

    private void LoadFromModel(AppSettings settings)
    {
        SelectedTheme = settings.Theme;
        SelectedFont = settings.FontFamily;
        FontSize = settings.FontSize;
        AutoSave = settings.AutoSave;
        AutoSaveInterval = settings.AutoSaveInterval;
        ConfirmBeforeDelete = settings.ConfirmBeforeDelete;
        ShowCompletedNotes = settings.ShowCompletedNotes;
    }

    private void SaveSettings()
    {
        var settings = new AppSettings
        {
            Theme = SelectedTheme,
            FontFamily = SelectedFont,
            FontSize = (int)FontSize,
            AutoSave = AutoSave,
            AutoSaveInterval = AutoSaveInterval,
            ConfirmBeforeDelete = ConfirmBeforeDelete,
            ShowCompletedNotes = ShowCompletedNotes
        };

        DataService.SaveSettings(settings);
        StatusMessage = "Settings saved successfully!";
    }

    private void ResetToDefaults()
    {
        LoadFromModel(new AppSettings());
        StatusMessage = "Reset to default values.";
    }
}