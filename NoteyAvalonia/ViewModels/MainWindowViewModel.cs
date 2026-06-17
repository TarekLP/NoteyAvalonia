using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NoteToolAvalonia.Models;
using NoteToolAvalonia.Services;

namespace NoteToolAvalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly NoteyService _service;

    [ObservableProperty] private ViewModelBase? _currentView;
    [ObservableProperty] private string _title = "Notey";
    [ObservableProperty] private bool _isSidebarVisible = true;
    [ObservableProperty] private string _currentFontFamily = "Inter";
    [ObservableProperty] private int _currentFontSize = 15;

    public MainWindowViewModel(NoteyService service)
    {
        _service = service;
        ApplyAppSettings();
    }

    public void ApplyAppSettings()
    {
        var settings = _service.LoadSettings();
        CurrentFontFamily = settings.FontFamily;
        CurrentFontSize = settings.FontSize;

        if (Application.Current != null)
        {
            Application.Current.RequestedThemeVariant = settings.Theme switch
            {
                "Light"  => ThemeVariant.Light,
                "Dark"   => ThemeVariant.Dark,
                _        => ThemeVariant.Default
            };
        }
    }

    public void NavigateToWelcome()
    {
        CurrentView = new WelcomeViewModel(_service);
        Title = "Notey";
    }

    public void NavigateToNoteEditor(NoteCard card)
    {
        CurrentView = new NoteEditorViewModel(card, _service);
        Title = $"Notey — {card.Title}";
    }

    public void NavigateToSettings()
    {
        CurrentView = new SettingsViewModel(_service);
        Title = "Notey — Settings";
    }

    public void NavigateToCredits()
    {
        CurrentView = new CreditsViewModel();
        Title = "Notey — Credits";
    }

    [RelayCommand] private void GoHome()     => NavigateToWelcome();
    [RelayCommand] private void GoSettings() => NavigateToSettings();
    [RelayCommand] private void GoCredits()  => NavigateToCredits();
    [RelayCommand] private void ToggleSidebar() => IsSidebarVisible = !IsSidebarVisible;
}
