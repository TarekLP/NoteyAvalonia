using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using NoteToolAvalonia.Services;
using NoteToolAvalonia.ViewModels;
using NoteToolAvalonia.Views;

namespace NoteToolAvalonia;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var dataService = new DataService();
            var navigationService = new NavigationService();
            var mainVm = new MainWindowViewModel(navigationService, dataService);
            var mainWindow = new MainWindow { DataContext = mainVm };
            navigationService.SetMainViewModel(mainVm);
            desktop.MainWindow = mainWindow;
            mainVm.NavigateToWelcome();
        }
        base.OnFrameworkInitializationCompleted();
    }
}
