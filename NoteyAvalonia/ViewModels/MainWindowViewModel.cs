using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NoteToolAvalonia.Models;
using NoteToolAvalonia.Services;
using System;
using System.Globalization;

namespace NoteToolAvalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
	private readonly NavigationService _navigation;

	public DataService DataService { get; }

	[ObservableProperty]
	private ViewModelBase? _currentView;

	[ObservableProperty]
	private string _title = "Notey";

	[ObservableProperty]
	private bool _isSidebarVisible = true;

	[ObservableProperty]
	private string _currentFontFamily = "Inter";

	[ObservableProperty]
	private int _currentFontSize = 14;

	public MainWindowViewModel(NavigationService navigation, DataService dataService)
	{
		_navigation = navigation;
		DataService = dataService;

		ApplyAppSettings();
		NavigateToWelcome();

	}

	public void ApplyAppSettings()
	{
		var settings = DataService.LoadSettings();

		CurrentFontFamily = settings.FontFamily;
		CurrentFontSize = settings.FontSize;

		if (Application.Current != null)
		{
			Application.Current.RequestedThemeVariant = settings.Theme switch
			{
				"Light" => ThemeVariant.Light,
				"Dark" => ThemeVariant.Dark,
				_ => ThemeVariant.Default
			};
		}
	}

	public void NavigateToWelcome()
	{
		CurrentView = new WelcomeViewModel(_navigation, DataService);
		Title = "Notey - Welcome";
	}

	public void NavigateToBoard(Board board)
	{
		CurrentView = new BoardViewModel(board, _navigation, DataService);
		Title = $"Notey - {board.Name}";
	}

	public void NavigateToNoteEditor(NoteCard card, Board board, BoardColumn column)
	{
		CurrentView = new NoteEditorViewModel(card, board, column, _navigation, DataService);
		Title = $"Notey - Editing: {card.Title}";
	}

	public void NavigateToSettings()
	{
		CurrentView = new SettingsViewModel(DataService, _navigation);
		Title = "Notey - Settings";
	}

	public void NavigateToCredits()
	{
		CurrentView = new CreditsViewModel();
		Title = "Notey - Credits";
	}
	public class BoolToDoubleConverter : IValueConverter
	{
		public double TrueValue { get; set; }
		public double FalseValue { get; set; }

		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is bool b)
				return b ? TrueValue : FalseValue;
			return FalseValue;
		}

		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
	[RelayCommand]
	private void GoHome() => NavigateToWelcome();

	[RelayCommand]
	private void GoSettings() => NavigateToSettings();
	
	[RelayCommand]
	private void GoCredits() => NavigateToCredits();

	[RelayCommand]
	private void ToggleSidebar() => IsSidebarVisible = !IsSidebarVisible;
}