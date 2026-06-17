using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NoteToolAvalonia.Models;
using NoteToolAvalonia.Services;

namespace NoteToolAvalonia.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly NoteyService _service;

    [ObservableProperty] private string _statusMessage       = "";
    [ObservableProperty] private string _selectedTheme       = "Dark";
    [ObservableProperty] private bool   _autoSave            = true;
    [ObservableProperty] private int    _autoSaveInterval    = 2;
    [ObservableProperty] private bool   _confirmBeforeDelete = true;
    [ObservableProperty] private bool   _showCompletedNotes  = true;

    // Storage location — shown read-only unless user browses to override
    [ObservableProperty] private string _dataFolderPath      = "";

    public List<string> Themes { get; } = new() { "Dark", "Light", "System" };

    public SettingsViewModel(NoteyService service)
    {
        _service        = service;
        _dataFolderPath = _service.DataFolder;   // populate from service on init

        var settings = _service.LoadSettings();
        LoadFromModel(settings);
        ApplyFontSettings();
    }

    // ── Load / Save ────────────────────────────────────────

    private void LoadFromModel(AppSettings s)
    {
        SelectedTheme       = s.Theme ?? "Dark";
        AutoSave            = s.AutoSave;
        AutoSaveInterval    = s.AutoSaveInterval;
        ConfirmBeforeDelete = s.ConfirmBeforeDelete;
        ShowCompletedNotes  = s.ShowCompletedNotes;
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

        // Re-apply theme / font immediately without restart
        if (Application.Current?.ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime
               { MainWindow.DataContext: MainWindowViewModel mainVm })
        {
            mainVm.ApplyAppSettings();
        }

        StatusMessage = "Settings saved!";
    }

    [RelayCommand]
    private void ResetDefaults()
    {
        LoadFromModel(new AppSettings
        {
            AutoSaveInterval = 2,
            FontSize         = 15,
            FontFamily       = "Inter",
            Theme            = "Dark"
        });
        StatusMessage = "Reset to defaults.";
    }

    [RelayCommand]
    private void GoBack()
    {
        if (Application.Current?.ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime
               { MainWindow.DataContext: MainWindowViewModel mainVm })
        {
            mainVm.NavigateToWelcome();
        }
    }

    // ── Browse for a custom notes folder ──────────────────

    [RelayCommand]
    private async Task BrowseFolder()
    {
        if (Application.Current?.ApplicationLifetime
            is not IClassicDesktopStyleApplicationLifetime { MainWindow: { } window })
            return;

        var topLevel = TopLevel.GetTopLevel(window);
        if (topLevel == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title       = "Select Notes Folder",
                AllowMultiple = false
            });

        if (folders.Count > 0)
            DataFolderPath = folders[0].Path.LocalPath;
    }

    // ── Font / theme side-effects ──────────────────────────

    private static void ApplyFontSettings()
    {
        if (Application.Current == null) return;
        Application.Current.Resources["AppFontFamily"] =
            new Avalonia.Media.FontFamily("Inter");
        Application.Current.Resources["AppFontSize"] = 15.0;
    }
}
