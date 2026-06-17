using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NoteToolAvalonia.Models;
using NoteToolAvalonia.Services;

namespace NoteToolAvalonia.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly NoteyService _service;

    [ObservableProperty] private string _statusMessage    = "";
    [ObservableProperty] private string _selectedTheme    = "Dark";
    [ObservableProperty] private bool   _autoSave         = true;
    [ObservableProperty] private int    _autoSaveInterval = 2;
    [ObservableProperty] private bool   _confirmBeforeDelete = true;
    [ObservableProperty] private bool   _showCompletedNotes  = true;
    [ObservableProperty] private string _dataFolderPath   = "";
    [ObservableProperty] private string _selectedStorageMode = "App Data";

    public List<string> Themes       { get; } = new() { "Dark", "Light", "System" };
    public List<string> StorageModes { get; } = new() { "App Data", "Custom Folder" };

    public bool IsCustomStorageSelected => SelectedStorageMode == "Custom Folder";

    public SettingsViewModel(NoteyService service)
    {
        _service = service;
        var settings = _service.LoadSettings();
        LoadFromModel(settings);
        ApplyFontSettings();
    }

    partial void OnSelectedStorageModeChanged(string value) =>
        OnPropertyChanged(nameof(IsCustomStorageSelected));

    private void LoadFromModel(AppSettings s)
    {
        SelectedTheme        = s.Theme ?? "Dark";
        AutoSave             = s.AutoSave;
        AutoSaveInterval     = s.AutoSaveInterval;
        ConfirmBeforeDelete  = s.ConfirmBeforeDelete;
        ShowCompletedNotes   = s.ShowCompletedNotes;
    }

    [RelayCommand]
    private void SaveSettings()
    {
        _service.SaveSettings(new AppSettings
        {
            Theme               = SelectedTheme,
            FontFamily          = "Inter",
            FontSize            = 15,
            AutoSave            = AutoSave,
            AutoSaveInterval    = AutoSaveInterval,
            ConfirmBeforeDelete = ConfirmBeforeDelete,
            ShowCompletedNotes  = ShowCompletedNotes
        });

        if (Application.Current?.ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime { MainWindow.DataContext: MainWindowViewModel mainVm })
        {
            mainVm.ApplyAppSettings();
        }

        StatusMessage = "Settings saved!";
    }

    [RelayCommand]
    private void ResetDefaults()
    {
        SelectedStorageMode = "App Data";
        LoadFromModel(new AppSettings { AutoSaveInterval = 2, FontSize = 15, FontFamily = "Inter", Theme = "Dark" });
        StatusMessage = "Reset to defaults.";
    }

    [RelayCommand]
    private void GoBack()
    {
        if (Application.Current?.ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime { MainWindow.DataContext: MainWindowViewModel mainVm })
        {
            mainVm.NavigateToWelcome();
        }
    }

    [RelayCommand]
    private async Task BrowseFolder()
    {
        if (Application.Current?.ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime { MainWindow: { } window })
        {
            var topLevel = TopLevel.GetTopLevel(window);
            if (topLevel == null) return;

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions { Title = "Select Data Folder", AllowMultiple = false });

            if (folders.Count > 0)
                DataFolderPath = folders[0].Path.LocalPath;
        }
    }

    private static void ApplyFontSettings()
    {
        if (Application.Current == null) return;
        Application.Current.Resources["AppFontFamily"] = new Avalonia.Media.FontFamily("Inter");
        Application.Current.Resources["AppFontSize"]   = 15.0;
    }
}
