using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NoteToolAvalonia.Models;
using NoteToolAvalonia.Services;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace NoteToolAvalonia.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
	public DataService DataService { get; }
	private readonly NavigationService _navigation;

	[ObservableProperty] private string _statusMessage = "";
	[ObservableProperty] private string _selectedTheme = "Dark";
	[ObservableProperty] private bool _autoSave;
	[ObservableProperty] private int _autoSaveInterval;
	[ObservableProperty] private bool _confirmBeforeDelete;
	[ObservableProperty] private bool _showCompletedNotes;
	[ObservableProperty] private string _dataFolderPath = "";

	[ObservableProperty] private string _selectedStorageMode = "App Data";
	public List<string> StorageModes { get; } = new() { "Executable Folder", "App Data", "Custom Folder" };

	public bool IsCustomStorageSelected => SelectedStorageMode == "Custom Folder";

	public List<string> Themes { get; } = new() { "Dark", "Light", "System" };

	public SettingsViewModel(DataService dataService, NavigationService navigation)
	{
		DataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
		_navigation = navigation;
		_dataFolderPath = DataService.DataFolder ?? "";

		var settings = DataService.LoadSettings();
		LoadFromModel(settings);
		ApplyFontSettings();
	}

	partial void OnSelectedStorageModeChanged(string value)
	{
		OnPropertyChanged(nameof(IsCustomStorageSelected));
	}

	private void LoadFromModel(AppSettings settings)
	{
		SelectedTheme = settings.Theme ?? "Dark";
		AutoSave = settings.AutoSave;
		AutoSaveInterval = settings.AutoSaveInterval;
		ConfirmBeforeDelete = settings.ConfirmBeforeDelete;
		ShowCompletedNotes = settings.ShowCompletedNotes;
		DataFolderPath = DataService.DataFolder ?? "";
	}

	[RelayCommand]
	private void SaveSettings()
	{
		var settings = new AppSettings
		{
			Theme = SelectedTheme,
			FontFamily = "Inter", 
			FontSize = 15, 
			AutoSave = AutoSave,
			AutoSaveInterval = AutoSaveInterval,
			ConfirmBeforeDelete = ConfirmBeforeDelete,
			ShowCompletedNotes = ShowCompletedNotes
		};

		DataService.SaveSettings(settings);

		if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			if (desktop.MainWindow?.DataContext is MainWindowViewModel mainVm)
			{
				mainVm.ApplyAppSettings();
			}
		}

		StatusMessage = "Settings saved and applied!";
	}

	[RelayCommand]
	private void ResetDefaults()
	{
		var defaults = new AppSettings 
		{ 
			AutoSaveInterval = 2,
			FontSize = 15,
			FontFamily = "Inter",
			Theme = "Dark"
		};
		SelectedStorageMode = "App Data";
		LoadFromModel(defaults);
		StatusMessage = "Reset to default values.";
	}

	[RelayCommand]
	private void GoBack() => _navigation.NavigateToWelcome();

	[RelayCommand]
	private async Task BrowseFolder()
	{
		if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } window })
		{
			var topLevel = TopLevel.GetTopLevel(window);
			if (topLevel == null) return;

			var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
			{
				Title = "Select Data Folder",
				AllowMultiple = false
			});

			if (folders.Count > 0)
			{
				DataFolderPath = folders[0].Path.LocalPath;
			}
		}
	}

	private void ApplyFontSettings()
	{
		if (Application.Current != null)
		{
			Application.Current.Resources["AppFontFamily"] = new Avalonia.Media.FontFamily("Inter");
			Application.Current.Resources["AppFontSize"] = 15.0; 
		}
	}
}