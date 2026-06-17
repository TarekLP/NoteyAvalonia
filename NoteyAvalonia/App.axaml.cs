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
            var noteyService = new NoteyService();
            var mainVm = new MainWindowViewModel(noteyService);
            noteyService.SetMainViewModel(mainVm);

            desktop.MainWindow = new MainWindow { DataContext = mainVm };
            mainVm.NavigateToWelcome();
        }
        base.OnFrameworkInitializationCompleted();
    }
}
