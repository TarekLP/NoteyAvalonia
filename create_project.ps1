# create_project.ps1 — Run in PowerShell to scaffold the entire project

$root = "NoteToolAvalonia"

# Create directory structure
$dirs = @(
    "$root",
    "$root/Models",
    "$root/Services",
    "$root/Converters",
    "$root/ViewModels",
    "$root/Views",
    "$root/Styles"
)
foreach ($d in $dirs) {
    New-Item -ItemType Directory -Force -Path $d | Out-Null
}

# ─── NoteToolAvalonia.csproj ───
@'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ApplicationManifest>app.manifest</ApplicationManifest>
    <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Avalonia" Version="11.3.11" />
    <PackageReference Include="Avalonia.Desktop" Version="11.3.11" />
    <PackageReference Include="Avalonia.Themes.Fluent" Version="11.3.11" />
    <PackageReference Include="Avalonia.Fonts.Inter" Version="11.3.11" />
    <!--Condition below is needed to remove Avalonia.Diagnostics package from build output in Release configuration.-->
    <PackageReference Include="Avalonia.Diagnostics" Version="11.3.11">
      <IncludeAssets Condition="'$(Configuration)' != 'Debug'">None</IncludeAssets>
      <PrivateAssets Condition="'$(Configuration)' != 'Debug'">All</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <Folder Include="Models\" />
  </ItemGroup>
</Project>
'@ | Set-Content "$root/NoteToolAvalonia.csproj" -Encoding UTF8

# ─── app.manifest ───
@'
<?xml version="1.0" encoding="utf-8"?>
<assembly manifestVersion="1.0" xmlns="urn:schemas-microsoft-com:asm.v1">
  <assemblyIdentity version="1.0.0.0" name="NoteToolAvalonia.Desktop"/>
  <compatibility xmlns="urn:schemas-microsoft-com:compatibility.v1">
    <application>
      <supportedOS Id="{8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a}" />
    </application>
  </compatibility>
</assembly>
'@ | Set-Content "$root/app.manifest" -Encoding UTF8

# ─── Program.cs ───
@'
using System;
using Avalonia;

namespace NoteToolAvalonia;

public static class Program
{
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
'@ | Set-Content "$root/Program.cs" -Encoding UTF8

# ─── App.axaml ───
@'
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="NoteToolAvalonia.App"
             RequestedThemeVariant="Dark">
  <Application.Styles>
    <FluentTheme />
    <StyleInclude Source="avares://NoteToolAvalonia/Styles/AppStyles.axaml" />
  </Application.Styles>
</Application>
'@ | Set-Content "$root/App.axaml" -Encoding UTF8

# ─── App.axaml.cs ───
@'
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
'@ | Set-Content "$root/App.axaml.cs" -Encoding UTF8

# ─── Models/Board.cs ───
@'
using System;
using System.Collections.Generic;

namespace NoteToolAvalonia.Models;

public class Board
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "Untitled Board";
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime LastModified { get; set; } = DateTime.Now;
    public List<BoardColumn> Columns { get; set; } = new();
}
'@ | Set-Content "$root/Models/Board.cs" -Encoding UTF8

# ─── Models/BoardColumn.cs ───
@'
using System;
using System.Collections.Generic;

namespace NoteToolAvalonia.Models;

public class BoardColumn
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = "New Column";
    public string Color { get; set; } = "#3498db";
    public int Order { get; set; }
    public List<NoteCard> Cards { get; set; } = new();
}
'@ | Set-Content "$root/Models/BoardColumn.cs" -Encoding UTF8

# ─── Models/NoteCard.cs ───
@'
using System;

namespace NoteToolAvalonia.Models;

public enum NotePriority
{
    None,
    Low,
    Medium,
    High,
    Critical
}

public class NoteCard
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = "New Note";
    public string Content { get; set; } = string.Empty;
    public NotePriority Priority { get; set; } = NotePriority.None;
    public string Tags { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime LastModified { get; set; } = DateTime.Now;
    public bool IsCompleted { get; set; }
    public string ColumnId { get; set; } = string.Empty;
}
'@ | Set-Content "$root/Models/NoteCard.cs" -Encoding UTF8

# ─── Models/AppSettings.cs ───
@'
namespace NoteToolAvalonia.Models;

public class AppSettings
{
    public string Theme { get; set; } = "Dark";
    public string AccentColor { get; set; } = "#3498db";
    public double FontSize { get; set; } = 14;
    public string FontFamily { get; set; } = "Inter";
    public bool AutoSave { get; set; } = true;
    public int AutoSaveIntervalSeconds { get; set; } = 30;
    public string DataFolderPath { get; set; } = string.Empty;
    public bool ConfirmBeforeDelete { get; set; } = true;
    public bool ShowCompletedNotes { get; set; } = true;
}
'@ | Set-Content "$root/Models/AppSettings.cs" -Encoding UTF8

# ─── Services/INavigationService.cs ───
@'
using NoteToolAvalonia.Models;

namespace NoteToolAvalonia.Services;

public interface INavigationService
{
    void NavigateToWelcome();
    void NavigateToBoard(Board board);
    void NavigateToNoteEditor(NoteCard card, Board board, BoardColumn column);
    void NavigateToSettings();
}
'@ | Set-Content "$root/Services/INavigationService.cs" -Encoding UTF8

# ─── Services/NavigationService.cs ───
@'
using NoteToolAvalonia.Models;
using NoteToolAvalonia.ViewModels;

namespace NoteToolAvalonia.Services;

public class NavigationService : INavigationService
{
    private MainWindowViewModel? _mainVm;

    public void SetMainViewModel(MainWindowViewModel vm)
    {
        _mainVm = vm;
    }

    public void NavigateToWelcome() => _mainVm?.NavigateToWelcome();
    public void NavigateToBoard(Board board) => _mainVm?.NavigateToBoard(board);
    public void NavigateToNoteEditor(NoteCard card, Board board, BoardColumn column) =>
        _mainVm?.NavigateToNoteEditor(card, board, column);
    public void NavigateToSettings() => _mainVm?.NavigateToSettings();
}
'@ | Set-Content "$root/Services/NavigationService.cs" -Encoding UTF8

# ─── Services/DataService.cs ───
@'
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using NoteToolAvalonia.Models;

namespace NoteToolAvalonia.Services;

public class DataService
{
    private readonly string _dataFolder;
    private readonly string _boardsFile;
    private readonly string _settingsFile;
    private readonly JsonSerializerOptions _jsonOptions;

    public DataService()
    {
        _dataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NoteToolAvalonia");
        Directory.CreateDirectory(_dataFolder);
        _boardsFile = Path.Combine(_dataFolder, "boards.json");
        _settingsFile = Path.Combine(_dataFolder, "settings.json");
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
    }

    public List<Board> LoadBoards()
    {
        if (!File.Exists(_boardsFile)) return new List<Board>();
        try
        {
            var json = File.ReadAllText(_boardsFile);
            return JsonSerializer.Deserialize<List<Board>>(json, _jsonOptions) ?? new List<Board>();
        }
        catch { return new List<Board>(); }
    }

    public void SaveBoards(List<Board> boards)
    {
        var json = JsonSerializer.Serialize(boards, _jsonOptions);
        File.WriteAllText(_boardsFile, json);
    }

    public void SaveBoard(Board board)
    {
        var boards = LoadBoards();
        var idx = boards.FindIndex(b => b.Id == board.Id);
        if (idx >= 0) boards[idx] = board;
        else boards.Add(board);
        SaveBoards(boards);
    }

    public void DeleteBoard(string boardId)
    {
        var boards = LoadBoards();
        boards.RemoveAll(b => b.Id == boardId);
        SaveBoards(boards);
    }

    public AppSettings LoadSettings()
    {
        if (!File.Exists(_settingsFile)) return new AppSettings();
        try
        {
            var json = File.ReadAllText(_settingsFile);
            return JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions) ?? new AppSettings();
        }
        catch { return new AppSettings(); }
    }

    public void SaveSettings(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, _jsonOptions);
        File.WriteAllText(_settingsFile, json);
    }

    public string DataFolder => _dataFolder;
}
'@ | Set-Content "$root/Services/DataService.cs" -Encoding UTF8

# ─── Converters/PriorityColorConverter.cs ───
@'
using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using NoteToolAvalonia.Models;

namespace NoteToolAvalonia.Converters;

public class PriorityColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is NotePriority priority)
        {
            return priority switch
            {
                NotePriority.Critical => new SolidColorBrush(Color.Parse("#e74c3c")),
                NotePriority.High     => new SolidColorBrush(Color.Parse("#e67e22")),
                NotePriority.Medium   => new SolidColorBrush(Color.Parse("#f39c12")),
                NotePriority.Low      => new SolidColorBrush(Color.Parse("#2ecc71")),
                _                     => new SolidColorBrush(Color.Parse("#95a5a6"))
            };
        }
        return new SolidColorBrush(Color.Parse("#95a5a6"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
'@ | Set-Content "$root/Converters/PriorityColorConverter.cs" -Encoding UTF8

# ─── Converters/BooleanToFontWeightConverter.cs ───
@'
using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace NoteToolAvalonia.Converters;

public class BooleanToFontWeightConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b && b) return FontWeight.Bold;
        return FontWeight.Normal;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
'@ | Set-Content "$root/Converters/BooleanToFontWeightConverter.cs" -Encoding UTF8

# ─── Styles/AppStyles.axaml ───
@'
<Styles xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

  <Style Selector="Border.card">
    <Setter Property="Background" Value="#2d2d30" />
    <Setter Property="CornerRadius" Value="8" />
    <Setter Property="Padding" Value="16" />
    <Setter Property="Margin" Value="4" />
    <Setter Property="BorderBrush" Value="#3e3e42" />
    <Setter Property="BorderThickness" Value="1" />
  </Style>
  <Style Selector="Border.card:pointerover">
    <Setter Property="BorderBrush" Value="#007acc" />
  </Style>

  <Style Selector="Border.kanban-column">
    <Setter Property="Background" Value="#252526" />
    <Setter Property="CornerRadius" Value="10" />
    <Setter Property="Padding" Value="12" />
    <Setter Property="Margin" Value="6" />
    <Setter Property="MinWidth" Value="280" />
    <Setter Property="MaxWidth" Value="340" />
  </Style>

  <Style Selector="Border.note-card">
    <Setter Property="Background" Value="#333337" />
    <Setter Property="CornerRadius" Value="6" />
    <Setter Property="Padding" Value="12" />
    <Setter Property="Margin" Value="0,4" />
    <Setter Property="BorderBrush" Value="#3e3e42" />
    <Setter Property="BorderThickness" Value="1" />
  </Style>
  <Style Selector="Border.note-card:pointerover">
    <Setter Property="BorderBrush" Value="#007acc" />
    <Setter Property="Background" Value="#3a3a3e" />
  </Style>

  <Style Selector="Border.sidebar">
    <Setter Property="Background" Value="#1e1e1e" />
    <Setter Property="Padding" Value="0" />
  </Style>

  <Style Selector="Button.sidebar-btn">
    <Setter Property="Background" Value="Transparent" />
    <Setter Property="Foreground" Value="#cccccc" />
    <Setter Property="HorizontalAlignment" Value="Stretch" />
    <Setter Property="HorizontalContentAlignment" Value="Left" />
    <Setter Property="Padding" Value="16,12" />
    <Setter Property="CornerRadius" Value="0" />
    <Setter Property="FontSize" Value="14" />
  </Style>
  <Style Selector="Button.sidebar-btn:pointerover /template/ ContentPresenter">
    <Setter Property="Background" Value="#2d2d30" />
  </Style>

  <Style Selector="Button.accent">
    <Setter Property="Background" Value="#007acc" />
    <Setter Property="Foreground" Value="White" />
    <Setter Property="CornerRadius" Value="6" />
    <Setter Property="Padding" Value="16,10" />
    <Setter Property="FontWeight" Value="SemiBold" />
  </Style>
  <Style Selector="Button.accent:pointerover /template/ ContentPresenter">
    <Setter Property="Background" Value="#1a8ad4" />
  </Style>

  <Style Selector="Button.danger">
    <Setter Property="Background" Value="#c0392b" />
    <Setter Property="Foreground" Value="White" />
    <Setter Property="CornerRadius" Value="6" />
    <Setter Property="Padding" Value="16,10" />
  </Style>
  <Style Selector="Button.danger:pointerover /template/ ContentPresenter">
    <Setter Property="Background" Value="#e74c3c" />
  </Style>

  <Style Selector="Button.ghost">
    <Setter Property="Background" Value="Transparent" />
    <Setter Property="Foreground" Value="#999" />
    <Setter Property="CornerRadius" Value="6" />
    <Setter Property="Padding" Value="10,6" />
  </Style>
  <Style Selector="Button.ghost:pointerover /template/ ContentPresenter">
    <Setter Property="Background" Value="#333" />
  </Style>

  <Style Selector="TextBlock.section-title">
    <Setter Property="FontSize" Value="22" />
    <Setter Property="FontWeight" Value="Bold" />
    <Setter Property="Foreground" Value="#ffffff" />
    <Setter Property="Margin" Value="0,0,0,12" />
  </Style>

  <Style Selector="TextBlock.subtitle">
    <Setter Property="FontSize" Value="13" />
    <Setter Property="Foreground" Value="#888" />
  </Style>

  <Style Selector="Border.priority-tag">
    <Setter Property="CornerRadius" Value="4" />
    <Setter Property="Padding" Value="8,3" />
    <Setter Property="Margin" Value="0,4,4,0" />
  </Style>

  <Style Selector="TextBox.editor">
    <Setter Property="Background" Value="#1e1e1e" />
    <Setter Property="Foreground" Value="#d4d4d4" />
    <Setter Property="BorderBrush" Value="#3e3e42" />
    <Setter Property="CornerRadius" Value="6" />
    <Setter Property="Padding" Value="12" />
    <Setter Property="FontFamily" Value="Cascadia Code, Consolas, monospace" />
  </Style>

</Styles>
'@ | Set-Content "$root/Styles/AppStyles.axaml" -Encoding UTF8

# ─── ViewModels/ViewModelBase.cs ───
@'
using CommunityToolkit.Mvvm.ComponentModel;

namespace NoteToolAvalonia.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
}
'@ | Set-Content "$root/ViewModels/ViewModelBase.cs" -Encoding UTF8

# ─── ViewModels/MainWindowViewModel.cs ───
@'
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NoteToolAvalonia.Models;
using NoteToolAvalonia.Services;

namespace NoteToolAvalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly NavigationService _navigation;
    public DataService DataService { get; }

    [ObservableProperty]
    private ViewModelBase? _currentView;

    [ObservableProperty]
    private string _title = "NoteTool";

    [ObservableProperty]
    private bool _isSidebarVisible = true;

    public MainWindowViewModel(NavigationService navigation, DataService dataService)
    {
        _navigation = navigation;
        DataService = dataService;
    }

    public void NavigateToWelcome()
    {
        CurrentView = new WelcomeViewModel(_navigation, DataService);
        Title = "NoteTool - Welcome";
    }

    public void NavigateToBoard(Board board)
    {
        CurrentView = new BoardViewModel(board, _navigation, DataService);
        Title = $"NoteTool - {board.Name}";
    }

    public void NavigateToNoteEditor(NoteCard card, Board board, BoardColumn column)
    {
        CurrentView = new NoteEditorViewModel(card, board, column, _navigation, DataService);
        Title = $"NoteTool - Editing: {card.Title}";
    }

    public void NavigateToSettings()
    {
        CurrentView = new SettingsViewModel(_navigation, DataService);
        Title = "NoteTool - Settings";
    }

    [RelayCommand]
    private void GoHome() => NavigateToWelcome();

    [RelayCommand]
    private void GoSettings() => NavigateToSettings();

    [RelayCommand]
    private void ToggleSidebar() => IsSidebarVisible = !IsSidebarVisible;
}
'@ | Set-Content "$root/ViewModels/MainWindowViewModel.cs" -Encoding UTF8

# ─── ViewModels/WelcomeViewModel.cs ───
@'
using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NoteToolAvalonia.Models;
using NoteToolAvalonia.Services;

namespace NoteToolAvalonia.ViewModels;

public partial class WelcomeViewModel : ViewModelBase
{
    private readonly NavigationService _navigation;
    private readonly DataService _dataService;

    [ObservableProperty]
    private ObservableCollection<Board> _boards = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<Board> _filteredBoards = new();

    [ObservableProperty]
    private string _newBoardName = string.Empty;

    [ObservableProperty]
    private bool _isCreatingBoard;

    public WelcomeViewModel(NavigationService navigation, DataService dataService)
    {
        _navigation = navigation;
        _dataService = dataService;
        LoadBoards();
    }

    private void LoadBoards()
    {
        var boards = _dataService.LoadBoards();
        Boards = new ObservableCollection<Board>(boards.OrderByDescending(b => b.LastModified));
        ApplyFilter();
        OnPropertyChanged(nameof(TotalBoards));
        OnPropertyChanged(nameof(TotalNotes));
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
            FilteredBoards = new ObservableCollection<Board>(Boards);
        else
            FilteredBoards = new ObservableCollection<Board>(
                Boards.Where(b =>
                    b.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    b.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase)));
    }

    [RelayCommand]
    private void OpenBoard(Board board)
    {
        board.LastModified = DateTime.Now;
        _dataService.SaveBoard(board);
        _navigation.NavigateToBoard(board);
    }

    [RelayCommand]
    private void ShowCreateBoard()
    {
        IsCreatingBoard = true;
        NewBoardName = string.Empty;
    }

    [RelayCommand]
    private void CancelCreateBoard()
    {
        IsCreatingBoard = false;
        NewBoardName = string.Empty;
    }

    [RelayCommand]
    private void CreateBoard()
    {
        if (string.IsNullOrWhiteSpace(NewBoardName)) return;
        var board = new Board
        {
            Name = NewBoardName.Trim(),
            Columns = new()
            {
                new BoardColumn { Title = "To Do",       Order = 0, Color = "#3498db" },
                new BoardColumn { Title = "In Progress", Order = 1, Color = "#f39c12" },
                new BoardColumn { Title = "Done",        Order = 2, Color = "#2ecc71" }
            }
        };
        _dataService.SaveBoard(board);
        IsCreatingBoard = false;
        NewBoardName = string.Empty;
        LoadBoards();
    }

    [RelayCommand]
    private void DeleteBoard(Board board)
    {
        _dataService.DeleteBoard(board.Id);
        LoadBoards();
    }

    [RelayCommand]
    private void OpenSettings() => _navigation.NavigateToSettings();

    public int TotalNotes => Boards.Sum(b => b.Columns.Sum(c => c.Cards.Count));
    public int TotalBoards => Boards.Count;
}
'@ | Set-Content "$root/ViewModels/WelcomeViewModel.cs" -Encoding UTF8

# ─── ViewModels/BoardViewModel.cs ───
@'
using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NoteToolAvalonia.Models;
using NoteToolAvalonia.Services;

namespace NoteToolAvalonia.ViewModels;

public partial class BoardViewModel : ViewModelBase
{
    private readonly NavigationService _navigation;
    private readonly DataService _dataService;

    [ObservableProperty]
    private Board _board;

    [ObservableProperty]
    private ObservableCollection<BoardColumn> _columns = new();

    [ObservableProperty]
    private string _newColumnTitle = string.Empty;

    [ObservableProperty]
    private bool _isAddingColumn;

    [ObservableProperty]
    private bool _isEditingBoardName;

    [ObservableProperty]
    private string _editBoardName = string.Empty;

    public BoardViewModel(Board board, NavigationService navigation, DataService dataService)
    {
        _navigation = navigation;
        _dataService = dataService;
        _board = board;
        Columns = new ObservableCollection<BoardColumn>(board.Columns.OrderBy(c => c.Order));
    }

    private void Save()
    {
        Board.Columns = Columns.ToList();
        Board.LastModified = DateTime.Now;
        _dataService.SaveBoard(Board);
    }

    [RelayCommand]
    private void ShowAddColumn()
    {
        IsAddingColumn = true;
        NewColumnTitle = string.Empty;
    }

    [RelayCommand]
    private void CancelAddColumn() => IsAddingColumn = false;

    [RelayCommand]
    private void AddColumn()
    {
        if (string.IsNullOrWhiteSpace(NewColumnTitle)) return;
        Columns.Add(new BoardColumn
        {
            Title = NewColumnTitle.Trim(),
            Order = Columns.Count,
            Color = "#3498db"
        });
        IsAddingColumn = false;
        NewColumnTitle = string.Empty;
        Save();
    }

    [RelayCommand]
    private void DeleteColumn(BoardColumn column)
    {
        Columns.Remove(column);
        for (int i = 0; i < Columns.Count; i++) Columns[i].Order = i;
        Save();
    }

    [RelayCommand]
    private void AddCard(BoardColumn column)
    {
        var card = new NoteCard { Title = "New Note", ColumnId = column.Id };
        column.Cards.Add(card);
        Save();
        var idx = Columns.IndexOf(column);
        Columns.RemoveAt(idx);
        Columns.Insert(idx, column);
        _navigation.NavigateToNoteEditor(card, Board, column);
    }

    [RelayCommand]
    private void OpenCard(NoteCard card)
    {
        var column = Columns.FirstOrDefault(c => c.Cards.Any(n => n.Id == card.Id));
        if (column != null) _navigation.NavigateToNoteEditor(card, Board, column);
    }

    [RelayCommand]
    private void DeleteCard(NoteCard card)
    {
        foreach (var col in Columns)
        {
            if (col.Cards.RemoveAll(c => c.Id == card.Id) > 0) break;
        }
        Save();
        RefreshColumns();
    }

    [RelayCommand]
    private void MoveCardLeft(NoteCard card) => MoveCard(card, -1);

    [RelayCommand]
    private void MoveCardRight(NoteCard card) => MoveCard(card, 1);

    private void MoveCard(NoteCard card, int direction)
    {
        var sourceCol = Columns.FirstOrDefault(c => c.Cards.Any(n => n.Id == card.Id));
        if (sourceCol == null) return;
        var sourceIdx = Columns.IndexOf(sourceCol);
        var targetIdx = sourceIdx + direction;
        if (targetIdx < 0 || targetIdx >= Columns.Count) return;
        var targetCol = Columns[targetIdx];
        sourceCol.Cards.RemoveAll(c => c.Id == card.Id);
        card.ColumnId = targetCol.Id;
        targetCol.Cards.Add(card);
        Save();
        RefreshColumns();
    }

    [RelayCommand]
    private void ToggleCardCompleted(NoteCard card)
    {
        card.IsCompleted = !card.IsCompleted;
        card.LastModified = DateTime.Now;
        Save();
        RefreshColumns();
    }

    private void RefreshColumns()
    {
        Columns = new ObservableCollection<BoardColumn>(Columns.ToList());
    }

    [RelayCommand]
    private void StartEditBoardName()
    {
        EditBoardName = Board.Name;
        IsEditingBoardName = true;
    }

    [RelayCommand]
    private void SaveBoardName()
    {
        if (!string.IsNullOrWhiteSpace(EditBoardName))
        {
            Board.Name = EditBoardName.Trim();
            Save();
        }
        IsEditingBoardName = false;
    }

    [RelayCommand]
    private void CancelEditBoardName() => IsEditingBoardName = false;

    [RelayCommand]
    private void GoBack() => _navigation.NavigateToWelcome();
}
'@ | Set-Content "$root/ViewModels/BoardViewModel.cs" -Encoding UTF8

# ─── ViewModels/NoteEditorViewModel.cs ───
@'
using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NoteToolAvalonia.Models;
using NoteToolAvalonia.Services;

namespace NoteToolAvalonia.ViewModels;

public partial class NoteEditorViewModel : ViewModelBase
{
    private readonly NavigationService _navigation;
    private readonly DataService _dataService;
    private readonly Board _board;
    private readonly BoardColumn _column;
    private readonly NoteCard _card;

    [ObservableProperty] private string _noteTitle;
    [ObservableProperty] private string _noteContent;
    [ObservableProperty] private NotePriority _notePriority;
    [ObservableProperty] private string _noteTags;
    [ObservableProperty] private bool _isCompleted;
    [ObservableProperty] private DateTime _createdAt;
    [ObservableProperty] private DateTime _lastModified;
    [ObservableProperty] private string _columnName;
    [ObservableProperty] private int _wordCount;
    [ObservableProperty] private int _charCount;
    [ObservableProperty] private bool _hasUnsavedChanges;

    public NotePriority[] PriorityValues => Enum.GetValues<NotePriority>();

    public NoteEditorViewModel(NoteCard card, Board board, BoardColumn column,
                               NavigationService navigation, DataService dataService)
    {
        _card = card;
        _board = board;
        _column = column;
        _navigation = navigation;
        _dataService = dataService;
        _noteTitle = card.Title;
        _noteContent = card.Content;
        _notePriority = card.Priority;
        _noteTags = card.Tags;
        _isCompleted = card.IsCompleted;
        _createdAt = card.CreatedAt;
        _lastModified = card.LastModified;
        _columnName = column.Title;
        UpdateCounts();
    }

    partial void OnNoteContentChanged(string value) { HasUnsavedChanges = true; UpdateCounts(); }
    partial void OnNoteTitleChanged(string value) => HasUnsavedChanges = true;
    partial void OnNotePriorityChanged(NotePriority value) => HasUnsavedChanges = true;
    partial void OnNoteTagsChanged(string value) => HasUnsavedChanges = true;
    partial void OnIsCompletedChanged(bool value) => HasUnsavedChanges = true;

    private void UpdateCounts()
    {
        CharCount = NoteContent?.Length ?? 0;
        WordCount = string.IsNullOrWhiteSpace(NoteContent) ? 0
            : NoteContent.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    [RelayCommand]
    private void Save()
    {
        _card.Title = NoteTitle;
        _card.Content = NoteContent;
        _card.Priority = NotePriority;
        _card.Tags = NoteTags;
        _card.IsCompleted = IsCompleted;
        _card.LastModified = DateTime.Now;
        LastModified = _card.LastModified;
        foreach (var col in _board.Columns)
        {
            var existing = col.Cards.FirstOrDefault(c => c.Id == _card.Id);
            if (existing != null)
            {
                col.Cards[col.Cards.IndexOf(existing)] = _card;
                break;
            }
        }
        _board.LastModified = DateTime.Now;
        _dataService.SaveBoard(_board);
        HasUnsavedChanges = false;
    }

    [RelayCommand]
    private void SaveAndGoBack() { Save(); _navigation.NavigateToBoard(_board); }

    [RelayCommand]
    private void GoBack()
    {
        if (HasUnsavedChanges) Save();
        _navigation.NavigateToBoard(_board);
    }

    [RelayCommand]
    private void Delete()
    {
        foreach (var col in _board.Columns) col.Cards.RemoveAll(c => c.Id == _card.Id);
        _board.LastModified = DateTime.Now;
        _dataService.SaveBoard(_board);
        _navigation.NavigateToBoard(_board);
    }

    [RelayCommand]
    private void InsertTimestamp()
    {
        NoteContent = (NoteContent ?? "") + $"\n[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ";
    }

    [RelayCommand]
    private void InsertCheckbox() { NoteContent = (NoteContent ?? "") + "\n☐ "; }

    [RelayCommand]
    private void InsertSeparator() { NoteContent = (NoteContent ?? "") + "\n────────────────────\n"; }
}
'@ | Set-Content "$root/ViewModels/NoteEditorViewModel.cs" -Encoding UTF8

# ─── ViewModels/SettingsViewModel.cs ───
@'
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
'@ | Set-Content "$root/ViewModels/SettingsViewModel.cs" -Encoding UTF8

# ─── Views/MainWindow.axaml ───
@'
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:NoteToolAvalonia.ViewModels"
        xmlns:views="using:NoteToolAvalonia.Views"
        x:Class="NoteToolAvalonia.Views.MainWindow"
        x:DataType="vm:MainWindowViewModel"
        Title="{Binding Title}"
        Width="1280" Height="800"
        MinWidth="900" MinHeight="600"
        Background="#1e1e1e"
        WindowStartupLocation="CenterScreen">

  <DockPanel>
    <Border Classes="sidebar" DockPanel.Dock="Left" Width="220"
            IsVisible="{Binding IsSidebarVisible}">
      <DockPanel>
        <StackPanel DockPanel.Dock="Top" Margin="16,20,16,16">
          <TextBlock Text="📝 NoteTool" FontSize="20" FontWeight="Bold" Foreground="White" />
          <TextBlock Text="Kanban Note Manager" FontSize="11" Foreground="#666" Margin="0,2,0,0" />
          <Separator Margin="0,14,0,0" Background="#333" />
        </StackPanel>
        <StackPanel DockPanel.Dock="Top" Spacing="2" Margin="4,0">
          <Button Classes="sidebar-btn" Command="{Binding GoHomeCommand}">
            <StackPanel Orientation="Horizontal" Spacing="10">
              <TextBlock Text="🏠" FontSize="16" />
              <TextBlock Text="Home" VerticalAlignment="Center" />
            </StackPanel>
          </Button>
          <Button Classes="sidebar-btn" Command="{Binding GoSettingsCommand}">
            <StackPanel Orientation="Horizontal" Spacing="10">
              <TextBlock Text="⚙️" FontSize="16" />
              <TextBlock Text="Settings" VerticalAlignment="Center" />
            </StackPanel>
          </Button>
        </StackPanel>
        <StackPanel DockPanel.Dock="Bottom" Margin="16,0,16,16">
          <Separator Background="#333" Margin="0,0,0,12" />
          <TextBlock Text="v1.0.0 — Avalonia" FontSize="10" Foreground="#555"
                     HorizontalAlignment="Center" />
        </StackPanel>
        <Panel />
      </DockPanel>
    </Border>

    <Button DockPanel.Dock="Left" Command="{Binding ToggleSidebarCommand}"
            Content="☰" FontSize="18" VerticalAlignment="Top"
            Margin="4,8,0,0" Classes="ghost" Padding="8,6" />

    <Border Padding="0">
      <ContentControl Content="{Binding CurrentView}">
        <ContentControl.DataTemplates>
          <DataTemplate DataType="{x:Type vm:WelcomeViewModel}">
            <views:WelcomeView />
          </DataTemplate>
          <DataTemplate DataType="{x:Type vm:BoardViewModel}">
            <views:BoardView />
          </DataTemplate>
          <DataTemplate DataType="{x:Type vm:NoteEditorViewModel}">
            <views:NoteEditorView />
          </DataTemplate>
          <DataTemplate DataType="{x:Type vm:SettingsViewModel}">
            <views:SettingsView />
          </DataTemplate>
        </ContentControl.DataTemplates>
      </ContentControl>
    </Border>
  </DockPanel>
</Window>
'@ | Set-Content "$root/Views/MainWindow.axaml" -Encoding UTF8

# ─── Views/MainWindow.axaml.cs ───
@'
using Avalonia.Controls;

namespace NoteToolAvalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow() { InitializeComponent(); }
}
'@ | Set-Content "$root/Views/MainWindow.axaml.cs" -Encoding UTF8

# ─── Views/WelcomeView.axaml ───
@'
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:NoteToolAvalonia.ViewModels"
             x:Class="NoteToolAvalonia.Views.WelcomeView"
             x:DataType="vm:WelcomeViewModel">

  <ScrollViewer Padding="32,24">
    <StackPanel MaxWidth="900" Spacing="20">

      <StackPanel Margin="0,10,0,20">
        <TextBlock Text="Welcome to NoteTool" FontSize="32" FontWeight="Bold" Foreground="White" />
        <TextBlock Text="Organize your thoughts with Kanban-style boards and notes."
                   FontSize="15" Foreground="#888" Margin="0,6,0,0" />
      </StackPanel>

      <WrapPanel Margin="0,0,0,8">
        <Border Classes="card" Margin="0,0,12,0" MinWidth="150">
          <StackPanel>
            <TextBlock Text="📋 Boards" Foreground="#888" FontSize="12" />
            <TextBlock Text="{Binding TotalBoards}" FontSize="28" FontWeight="Bold" Foreground="White" />
          </StackPanel>
        </Border>
        <Border Classes="card" MinWidth="150">
          <StackPanel>
            <TextBlock Text="📝 Notes" Foreground="#888" FontSize="12" />
            <TextBlock Text="{Binding TotalNotes}" FontSize="28" FontWeight="Bold" Foreground="White" />
          </StackPanel>
        </Border>
      </WrapPanel>

      <DockPanel>
        <Button DockPanel.Dock="Right" Classes="accent" Command="{Binding ShowCreateBoardCommand}" Margin="12,0,0,0">
          <StackPanel Orientation="Horizontal" Spacing="6">
            <TextBlock Text="＋" FontSize="18" />
            <TextBlock Text="New Board" VerticalAlignment="Center" />
          </StackPanel>
        </Button>
        <TextBox Watermark="🔍 Search boards..." Text="{Binding SearchText, Mode=TwoWay}"
                 CornerRadius="6" Padding="12,10" />
      </DockPanel>

      <Border Classes="card" IsVisible="{Binding IsCreatingBoard}" Background="#2a2a2e">
        <StackPanel Spacing="12">
          <TextBlock Text="Create New Board" FontSize="16" FontWeight="SemiBold" Foreground="White" />
          <TextBox Watermark="Board name..." Text="{Binding NewBoardName, Mode=TwoWay}" Padding="12,10" CornerRadius="6" />
          <StackPanel Orientation="Horizontal" Spacing="8">
            <Button Classes="accent" Command="{Binding CreateBoardCommand}" Content="Create" />
            <Button Classes="ghost" Command="{Binding CancelCreateBoardCommand}" Content="Cancel" />
          </StackPanel>
        </StackPanel>
      </Border>

      <TextBlock Text="Your Boards" Classes="section-title" Margin="0,8,0,0" />

      <Border Classes="card" IsVisible="{Binding !FilteredBoards.Count}"
              HorizontalAlignment="Center" Margin="0,30">
        <StackPanel HorizontalAlignment="Center" Spacing="8">
          <TextBlock Text="📭" FontSize="48" HorizontalAlignment="Center" />
          <TextBlock Text="No boards yet" FontSize="18" Foreground="#888" HorizontalAlignment="Center" />
          <TextBlock Text="Click 'New Board' to get started!" FontSize="13" Foreground="#666" HorizontalAlignment="Center" />
        </StackPanel>
      </Border>

      <ItemsControl ItemsSource="{Binding FilteredBoards}">
        <ItemsControl.ItemTemplate>
          <DataTemplate>
            <Border Classes="card" Margin="0,0,0,4" Cursor="Hand">
              <DockPanel>
                <Button DockPanel.Dock="Right" Classes="ghost" VerticalAlignment="Top"
                        Command="{Binding $parent[ItemsControl].((vm:WelcomeViewModel)DataContext).DeleteBoardCommand}"
                        CommandParameter="{Binding}" Content="🗑" FontSize="16" ToolTip.Tip="Delete board" />
                <Border Background="Transparent" Cursor="Hand" Tapped="BoardCard_Tapped">
                  <StackPanel Spacing="6">
                    <TextBlock Text="{Binding Name}" FontSize="18" FontWeight="SemiBold" Foreground="White" />
                    <StackPanel Orientation="Horizontal" Spacing="16">
                      <TextBlock Foreground="#888" FontSize="12">
                        <TextBlock.Text>
                          <MultiBinding StringFormat="{}📁 {0} columns">
                            <Binding Path="Columns.Count" />
                          </MultiBinding>
                        </TextBlock.Text>
                      </TextBlock>
                      <TextBlock Foreground="#888" FontSize="12">
                        <TextBlock.Text>
                          <MultiBinding StringFormat="{}🕐 {0:MMM dd, yyyy}">
                            <Binding Path="LastModified" />
                          </MultiBinding>
                        </TextBlock.Text>
                      </TextBlock>
                    </StackPanel>
                  </StackPanel>
                </Border>
              </DockPanel>
            </Border>
          </DataTemplate>
        </ItemsControl.ItemTemplate>
      </ItemsControl>

    </StackPanel>
  </ScrollViewer>
</UserControl>
'@ | Set-Content "$root/Views/WelcomeView.axaml" -Encoding UTF8

# ─── Views/WelcomeView.axaml.cs ───
@'
using Avalonia.Controls;
using Avalonia.Input;
using NoteToolAvalonia.Models;
using NoteToolAvalonia.ViewModels;

namespace NoteToolAvalonia.Views;

public partial class WelcomeView : UserControl
{
    public WelcomeView() { InitializeComponent(); }

    private void BoardCard_Tapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border border && border.DataContext is Board board)
        {
            if (DataContext is WelcomeViewModel vm)
                vm.OpenBoardCommand.Execute(board);
        }
    }
}
'@ | Set-Content "$root/Views/WelcomeView.axaml.cs" -Encoding UTF8

# ─── Views/BoardView.axaml ───
@'
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:NoteToolAvalonia.ViewModels"
             xmlns:models="using:NoteToolAvalonia.Models"
             xmlns:conv="using:NoteToolAvalonia.Converters"
             x:Class="NoteToolAvalonia.Views.BoardView"
             x:DataType="vm:BoardViewModel">

  <UserControl.Resources>
    <conv:PriorityColorConverter x:Key="PriorityColorConverter" />
  </UserControl.Resources>

  <DockPanel>
    <Border DockPanel.Dock="Top" Background="#252526" Padding="20,12">
      <DockPanel>
        <StackPanel Orientation="Horizontal" Spacing="12" DockPanel.Dock="Left">
          <Button Classes="ghost" Command="{Binding GoBackCommand}" Content="← Back" FontSize="13" />
          <TextBlock Text="│" Foreground="#444" VerticalAlignment="Center" />
          <StackPanel Orientation="Horizontal" IsVisible="{Binding !IsEditingBoardName}">
            <TextBlock Text="{Binding Board.Name}" FontSize="20" FontWeight="Bold" Foreground="White" VerticalAlignment="Center" />
            <Button Classes="ghost" Content="✏️" FontSize="12" Command="{Binding StartEditBoardNameCommand}" Margin="8,0,0,0" VerticalAlignment="Center" />
          </StackPanel>
          <StackPanel Orientation="Horizontal" Spacing="6" IsVisible="{Binding IsEditingBoardName}">
            <TextBox Text="{Binding EditBoardName, Mode=TwoWay}" Width="250" Padding="8,6" CornerRadius="4" />
            <Button Classes="accent" Content="Save" Padding="12,6" Command="{Binding SaveBoardNameCommand}" />
            <Button Classes="ghost" Content="Cancel" Command="{Binding CancelEditBoardNameCommand}" />
          </StackPanel>
        </StackPanel>
        <StackPanel DockPanel.Dock="Right" Orientation="Horizontal" Spacing="8">
          <StackPanel Orientation="Horizontal" Spacing="6" IsVisible="{Binding IsAddingColumn}">
            <TextBox Text="{Binding NewColumnTitle, Mode=TwoWay}" Watermark="Column name..." Width="180" Padding="8,6" CornerRadius="4" />
            <Button Classes="accent" Content="Add" Command="{Binding AddColumnCommand}" Padding="12,6" />
            <Button Classes="ghost" Content="✕" Command="{Binding CancelAddColumnCommand}" />
          </StackPanel>
          <Button Classes="accent" Command="{Binding ShowAddColumnCommand}" IsVisible="{Binding !IsAddingColumn}">
            <StackPanel Orientation="Horizontal" Spacing="6">
              <TextBlock Text="＋" FontSize="16" />
              <TextBlock Text="Add Column" VerticalAlignment="Center" />
            </StackPanel>
          </Button>
        </StackPanel>
        <Panel />
      </DockPanel>
    </Border>

    <ScrollViewer HorizontalScrollBarVisibility="Auto" VerticalScrollBarVisibility="Disabled" Padding="14,14">
      <ItemsControl ItemsSource="{Binding Columns}">
        <ItemsControl.ItemsPanel>
          <ItemsPanelTemplate>
            <StackPanel Orientation="Horizontal" Spacing="0" />
          </ItemsPanelTemplate>
        </ItemsControl.ItemsPanel>
        <ItemsControl.ItemTemplate>
          <DataTemplate x:DataType="models:BoardColumn">
            <Border Classes="kanban-column">
              <DockPanel>
                <DockPanel DockPanel.Dock="Top" Margin="0,0,0,10">
                  <Button DockPanel.Dock="Right" Classes="ghost" Content="🗑" FontSize="13" ToolTip.Tip="Delete column"
                          Command="{Binding $parent[ItemsControl].((vm:BoardViewModel)DataContext).DeleteColumnCommand}"
                          CommandParameter="{Binding}" />
                  <StackPanel Orientation="Horizontal" Spacing="8">
                    <Border Width="4" Height="20" CornerRadius="2" Background="{Binding Color}" />
                    <TextBlock Text="{Binding Title}" FontSize="15" FontWeight="SemiBold" Foreground="White" VerticalAlignment="Center" />
                    <Border Background="#444" CornerRadius="10" Padding="8,2" VerticalAlignment="Center">
                      <TextBlock Text="{Binding Cards.Count}" FontSize="11" Foreground="#aaa" />
                    </Border>
                  </StackPanel>
                </DockPanel>
                <Button DockPanel.Dock="Bottom" Classes="ghost" HorizontalAlignment="Stretch" HorizontalContentAlignment="Center"
                        Command="{Binding $parent[ItemsControl].((vm:BoardViewModel)DataContext).AddCardCommand}"
                        CommandParameter="{Binding}" Margin="0,8,0,0">
                  <StackPanel Orientation="Horizontal" Spacing="6">
                    <TextBlock Text="＋" FontSize="14" />
                    <TextBlock Text="Add Note" FontSize="13" />
                  </StackPanel>
                </Button>
                <ScrollViewer VerticalScrollBarVisibility="Auto" HorizontalScrollBarVisibility="Disabled">
                  <ItemsControl ItemsSource="{Binding Cards}">
                    <ItemsControl.ItemTemplate>
                      <DataTemplate x:DataType="models:NoteCard">
                        <Border Classes="note-card" Cursor="Hand" Tapped="NoteCard_Tapped">
                          <DockPanel>
                            <StackPanel DockPanel.Dock="Bottom" Orientation="Horizontal" Spacing="4" Margin="0,8,0,0" HorizontalAlignment="Right">
                              <Button Classes="ghost" Content="◀" FontSize="10" Padding="6,3" ToolTip.Tip="Move left"
                                      Command="{Binding $parent[ItemsControl].$parent[ItemsControl].((vm:BoardViewModel)DataContext).MoveCardLeftCommand}"
                                      CommandParameter="{Binding}" />
                              <Button Classes="ghost" Content="▶" FontSize="10" Padding="6,3" ToolTip.Tip="Move right"
                                      Command="{Binding $parent[ItemsControl].$parent[ItemsControl].((vm:BoardViewModel)DataContext).MoveCardRightCommand}"
                                      CommandParameter="{Binding}" />
                              <Button Classes="ghost" Content="✓" FontSize="10" Padding="6,3" ToolTip.Tip="Toggle complete"
                                      Command="{Binding $parent[ItemsControl].$parent[ItemsControl].((vm:BoardViewModel)DataContext).ToggleCardCompletedCommand}"
                                      CommandParameter="{Binding}" />
                              <Button Classes="ghost" Content="🗑" FontSize="10" Padding="6,3" ToolTip.Tip="Delete"
                                      Command="{Binding $parent[ItemsControl].$parent[ItemsControl].((vm:BoardViewModel)DataContext).DeleteCardCommand}"
                                      CommandParameter="{Binding}" />
                            </StackPanel>
                            <StackPanel Spacing="4">
                              <Border Classes="priority-tag"
                                      Background="{Binding Priority, Converter={StaticResource PriorityColorConverter}}"
                                      HorizontalAlignment="Left">
                                <TextBlock Text="{Binding Priority}" FontSize="10" Foreground="White" FontWeight="Bold" />
                              </Border>
                              <TextBlock Text="{Binding Title}" FontSize="14" FontWeight="SemiBold" Foreground="White" />
                              <TextBlock Text="{Binding Content}" FontSize="12" Foreground="#999" MaxLines="3"
                                         TextTrimming="CharacterEllipsis" TextWrapping="Wrap"
                                         IsVisible="{Binding Content.Length}" />
                              <TextBlock Text="{Binding Tags, StringFormat='🏷 {0}'}" FontSize="11" Foreground="#666"
                                         IsVisible="{Binding Tags.Length}" Margin="0,4,0,0" />
                            </StackPanel>
                          </DockPanel>
                        </Border>
                      </DataTemplate>
                    </ItemsControl.ItemTemplate>
                  </ItemsControl>
                </ScrollViewer>
              </DockPanel>
            </Border>
          </DataTemplate>
        </ItemsControl.ItemTemplate>
      </ItemsControl>
    </ScrollViewer>
  </DockPanel>
</UserControl>
'@ | Set-Content "$root/Views/BoardView.axaml" -Encoding UTF8

# ─── Views/BoardView.axaml.cs ───
@'
using Avalonia.Controls;
using Avalonia.Input;
using NoteToolAvalonia.Models;
using NoteToolAvalonia.ViewModels;

namespace NoteToolAvalonia.Views;

public partial class BoardView : UserControl
{
    public BoardView() { InitializeComponent(); }

    private void NoteCard_Tapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border border && border.DataContext is NoteCard card)
        {
            if (DataContext is BoardViewModel vm)
                vm.OpenCardCommand.Execute(card);
        }
    }
}
'@ | Set-Content "$root/Views/BoardView.axaml.cs" -Encoding UTF8

# ─── Views/NoteEditorView.axaml ───
@'
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:NoteToolAvalonia.ViewModels"
             xmlns:conv="using:NoteToolAvalonia.Converters"
             x:Class="NoteToolAvalonia.Views.NoteEditorView"
             x:DataType="vm:NoteEditorViewModel">

  <UserControl.Resources>
    <conv:PriorityColorConverter x:Key="PriorityColorConverter" />
  </UserControl.Resources>

  <DockPanel>
    <Border DockPanel.Dock="Top" Background="#252526" Padding="16,10">
      <DockPanel>
        <StackPanel DockPanel.Dock="Left" Orientation="Horizontal" Spacing="8">
          <Button Classes="ghost" Content="← Back" Command="{Binding GoBackCommand}" />
          <TextBlock Text="│" Foreground="#444" VerticalAlignment="Center" />
          <TextBlock Text="📝 Note Editor" FontSize="16" FontWeight="SemiBold" Foreground="White" VerticalAlignment="Center" />
          <TextBlock Text="{Binding ColumnName, StringFormat='  in column: {0}'}" FontSize="12" Foreground="#666" VerticalAlignment="Center" />
        </StackPanel>
        <StackPanel DockPanel.Dock="Right" Orientation="Horizontal" Spacing="6">
          <TextBlock Text="● unsaved" FontSize="11" Foreground="#e67e22" VerticalAlignment="Center"
                     IsVisible="{Binding HasUnsavedChanges}" Margin="0,0,8,0" />
          <Button Classes="accent" Content="💾 Save" Command="{Binding SaveCommand}" />
          <Button Classes="danger" Content="🗑 Delete" Command="{Binding DeleteCommand}" />
        </StackPanel>
        <Panel />
      </DockPanel>
    </Border>

    <Border DockPanel.Dock="Bottom" Background="#1a1a1c" Padding="16,6">
      <DockPanel>
        <StackPanel DockPanel.Dock="Left" Orientation="Horizontal" Spacing="20">
          <TextBlock Text="{Binding WordCount, StringFormat='Words: {0}'}" FontSize="11" Foreground="#666" />
          <TextBlock Text="{Binding CharCount, StringFormat='Chars: {0}'}" FontSize="11" Foreground="#666" />
        </StackPanel>
        <StackPanel DockPanel.Dock="Right" Orientation="Horizontal" Spacing="20">
          <TextBlock Text="{Binding CreatedAt, StringFormat='Created: {0:g}'}" FontSize="11" Foreground="#666" />
          <TextBlock Text="{Binding LastModified, StringFormat='Modified: {0:g}'}" FontSize="11" Foreground="#666" />
        </StackPanel>
        <Panel />
      </DockPanel>
    </Border>

    <Grid ColumnDefinitions="320,*">
      <ScrollViewer Grid.Column="0" Padding="20,16">
        <StackPanel Spacing="16">
          <StackPanel Spacing="4">
            <TextBlock Text="TITLE" FontSize="11" FontWeight="Bold" Foreground="#888" />
            <TextBox Text="{Binding NoteTitle, Mode=TwoWay}" Watermark="Note title..." Padding="10,8" CornerRadius="4" FontSize="16" FontWeight="SemiBold" />
          </StackPanel>
          <StackPanel Spacing="4">
            <TextBlock Text="PRIORITY" FontSize="11" FontWeight="Bold" Foreground="#888" />
            <ComboBox ItemsSource="{Binding PriorityValues}" SelectedItem="{Binding NotePriority, Mode=TwoWay}" HorizontalAlignment="Stretch" Padding="10,8" CornerRadius="4" />
            <Border Classes="priority-tag" Background="{Binding NotePriority, Converter={StaticResource PriorityColorConverter}}" HorizontalAlignment="Left" Margin="0,4,0,0">
              <TextBlock Text="{Binding NotePriority}" FontSize="11" Foreground="White" FontWeight="Bold" />
            </Border>
          </StackPanel>
          <StackPanel Spacing="4">
            <TextBlock Text="TAGS" FontSize="11" FontWeight="Bold" Foreground="#888" />
            <TextBox Text="{Binding NoteTags, Mode=TwoWay}" Watermark="e.g. work, urgent, ideas" Padding="10,8" CornerRadius="4" />
          </StackPanel>
          <CheckBox IsChecked="{Binding IsCompleted, Mode=TwoWay}" Content="Mark as completed" Foreground="#ccc" />
          <Separator Background="#333" Margin="0,8" />
          <StackPanel Spacing="4">
            <TextBlock Text="QUICK INSERT" FontSize="11" FontWeight="Bold" Foreground="#888" />
            <WrapPanel>
              <Button Classes="ghost" Content="📅 Timestamp" Command="{Binding InsertTimestampCommand}" Margin="0,0,6,6" />
              <Button Classes="ghost" Content="☐ Checkbox" Command="{Binding InsertCheckboxCommand}" Margin="0,0,6,6" />
              <Button Classes="ghost" Content="── Separator" Command="{Binding InsertSeparatorCommand}" Margin="0,0,6,6" />
            </WrapPanel>
          </StackPanel>
        </StackPanel>
      </ScrollViewer>
      <Border Grid.Column="0" Width="1" Background="#333" HorizontalAlignment="Right" />
      <DockPanel Grid.Column="1">
        <TextBlock DockPanel.Dock="Top" Text="CONTENT" FontSize="11" FontWeight="Bold" Foreground="#888" Margin="20,16,20,8" />
        <TextBox Text="{Binding NoteContent, Mode=TwoWay}" AcceptsReturn="True" TextWrapping="Wrap"
                 Classes="editor" Margin="16,0,16,16" FontSize="14" Watermark="Start writing your note here..." />
      </DockPanel>
    </Grid>
  </DockPanel>
</UserControl>
'@ | Set-Content "$root/Views/NoteEditorView.axaml" -Encoding UTF8

# ─── Views/NoteEditorView.axaml.cs ───
@'
using Avalonia.Controls;

namespace NoteToolAvalonia.Views;

public partial class NoteEditorView : UserControl
{
    public NoteEditorView() { InitializeComponent(); }
}
'@ | Set-Content "$root/Views/NoteEditorView.axaml.cs" -Encoding UTF8

# ─── Views/SettingsView.axaml ───
@'
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:NoteToolAvalonia.ViewModels"
             x:Class="NoteToolAvalonia.Views.SettingsView"
             x:DataType="vm:SettingsViewModel">

  <ScrollViewer Padding="32,24">
    <StackPanel MaxWidth="700" Spacing="20">

      <DockPanel>
        <Button DockPanel.Dock="Left" Classes="ghost" Content="← Back" Command="{Binding GoBackCommand}" />
        <TextBlock Text="⚙️ Settings" FontSize="28" FontWeight="Bold" Foreground="White"
                   VerticalAlignment="Center" Margin="16,0,0,0" />
        <Panel />
      </DockPanel>

      <Border Classes="card">
        <StackPanel Spacing="14">
          <TextBlock Text="🎨 Appearance" Classes="section-title" FontSize="18" />
          <DockPanel>
            <TextBlock Text="Theme" Width="160" Foreground="#ccc" VerticalAlignment="Center" />
            <ComboBox ItemsSource="{Binding Themes}" SelectedItem="{Binding SelectedTheme, Mode=TwoWay}" MinWidth="200" />
          </DockPanel>
          <DockPanel>
            <TextBlock Text="Accent Color" Width="160" Foreground="#ccc" VerticalAlignment="Center" />
            <ComboBox ItemsSource="{Binding AccentColors}" SelectedItem="{Binding AccentColor, Mode=TwoWay}" MinWidth="200">
              <ComboBox.ItemTemplate>
                <DataTemplate>
                  <StackPanel Orientation="Horizontal" Spacing="8">
                    <Border Width="16" Height="16" CornerRadius="3" Background="{Binding}" />
                    <TextBlock Text="{Binding}" VerticalAlignment="Center" />
                  </StackPanel>
                </DataTemplate>
              </ComboBox.ItemTemplate>
            </ComboBox>
          </DockPanel>
          <DockPanel>
            <TextBlock Text="Font" Width="160" Foreground="#ccc" VerticalAlignment="Center" />
            <ComboBox ItemsSource="{Binding Fonts}" SelectedItem="{Binding SelectedFont, Mode=TwoWay}" MinWidth="200" />
          </DockPanel>
          <DockPanel>
            <TextBlock Text="{Binding FontSize, StringFormat='Font Size: {0:F0}'}" Width="160" Foreground="#ccc" VerticalAlignment="Center" />
            <Slider Value="{Binding FontSize, Mode=TwoWay}" Minimum="10" Maximum="24" TickFrequency="1" IsSnapToTickEnabled="True" MinWidth="200" />
          </DockPanel>
        </StackPanel>
      </Border>

      <Border Classes="card">
        <StackPanel Spacing="14">
          <TextBlock Text="⚡ Behavior" Classes="section-title" FontSize="18" />
          <CheckBox IsChecked="{Binding AutoSave, Mode=TwoWay}" Content="Auto-save notes" Foreground="#ccc" />
          <DockPanel IsEnabled="{Binding AutoSave}">
            <TextBlock Text="{Binding AutoSaveInterval, StringFormat='Auto-save interval: {0}s'}" Width="220" Foreground="#ccc" VerticalAlignment="Center" />
            <Slider Value="{Binding AutoSaveInterval, Mode=TwoWay}" Minimum="5" Maximum="120" TickFrequency="5" IsSnapToTickEnabled="True" MinWidth="200" />
          </DockPanel>
          <CheckBox IsChecked="{Binding ConfirmBeforeDelete, Mode=TwoWay}" Content="Confirm before deleting" Foreground="#ccc" />
          <CheckBox IsChecked="{Binding ShowCompletedNotes, Mode=TwoWay}" Content="Show completed notes" Foreground="#ccc" />
        </StackPanel>
      </Border>

      <Border Classes="card">
        <StackPanel Spacing="10">
          <TextBlock Text="📁 Data Storage" Classes="section-title" FontSize="18" />
          <DockPanel>
            <TextBlock Text="Data folder:" Foreground="#888" Width="100" VerticalAlignment="Center" />
            <TextBox Text="{Binding DataFolderPath}" IsReadOnly="True" Foreground="#999" Padding="8,6" CornerRadius="4" />
          </DockPanel>
        </StackPanel>
      </Border>

      <StackPanel Orientation="Horizontal" Spacing="12">
        <Button Classes="accent" Content="💾 Save Settings" Command="{Binding SaveSettingsCommand}" />
        <Button Classes="ghost" Content="↩ Reset Defaults" Command="{Binding ResetDefaultsCommand}" />
      </StackPanel>

      <TextBlock Text="{Binding StatusMessage}" FontSize="13" Foreground="#2ecc71"
                 IsVisible="{Binding StatusMessage.Length}" />

    </StackPanel>
  </ScrollViewer>
</UserControl>
'@ | Set-Content "$root/Views/SettingsView.axaml" -Encoding UTF8

# ─── Views/SettingsView.axaml.cs ───
@'
using Avalonia.Controls;

namespace NoteToolAvalonia.Views;

public partial class SettingsView : UserControl
{
    public SettingsView() { InitializeComponent(); }
}
'@ | Set-Content "$root/Views/SettingsView.axaml.cs" -Encoding UTF8




