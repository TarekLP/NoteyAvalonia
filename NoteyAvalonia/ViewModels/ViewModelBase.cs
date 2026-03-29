using CommunityToolkit.Mvvm.ComponentModel;

namespace NoteToolAvalonia.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
	public string CurrentFontFamily { get; set; } = "Segoe UI";
	public double CurrentFontSize { get; set; } = 14;
}
