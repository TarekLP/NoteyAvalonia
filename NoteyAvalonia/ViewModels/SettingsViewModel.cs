using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NoteToolAvalonia.Models;
using NoteToolAvalonia.Services;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace NoteToolAvalonia.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
	public DataService DataService { get; }
	private readonly NavigationService _navigation;

	[ObservableProperty] private string _statusMessage = "";
	[ObservableProperty] private string _selectedTheme;
	[ObservableProperty] private bool _autoSave;
	[ObservableProperty] private int _autoSaveInterval;
	[ObservableProperty] private bool _confirmBeforeDelete;
	[ObservableProperty] private bool _showCompletedNotes;
	[ObservableProperty] private string _dataFolderPath;

	public List<string> Themes { get; } = new() { "Dark", "Light", "System" };

	public SettingsViewModel(DataService dataService, NavigationService navigation)
	{
		DataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
		_navigation = navigation;
		_dataFolderPath = DataService.DataFolder;

		var settings = DataService.LoadSettings();
		LoadFromModel(settings);
		ApplyFontSettings();
	}

	private void LoadFromModel(AppSettings settings)
	{
		SelectedTheme = settings.Theme;
		AutoSave = settings.AutoSave;
		AutoSaveInterval = settings.AutoSaveInterval;
		ConfirmBeforeDelete = settings.ConfirmBeforeDelete;
		ShowCompletedNotes = settings.ShowCompletedNotes;
		DataFolderPath = DataService.DataFolder;
	}

	[RelayCommand]
	private void SaveSettings()
	{
		var settings = new AppSettings
		{
			Theme = SelectedTheme,
			FontFamily = "Inter", // Keep for backward compatibility but fixed
			FontSize = 15, // Fixed at 15
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
			FontFamily = "Inter"
		};
		LoadFromModel(defaults);
		StatusMessage = "Reset to default values.";
	}

	[RelayCommand]
	private void GoBack() => _navigation.NavigateToWelcome();

	[RelayCommand]
	private async void BrowseFolder()
	{
		if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			var dialog = new OpenFolderDialog
			{
				Title = "Select Data Folder",
				Directory = DataFolderPath
			};

			string? result = await dialog.ShowAsync(desktop.MainWindow);
			if (!string.IsNullOrEmpty(result))
			{
				DataFolderPath = result;
			}
		}
	}

	private void ApplyFontSettings()
	{
		if (Application.Current != null)
		{
			Application.Current.Resources["AppFontFamily"] = new Avalonia.Media.FontFamily("Inter");
			Application.Current.Resources["AppFontSize"] = 15.0; // Fixed at 15
		}
	}
}
