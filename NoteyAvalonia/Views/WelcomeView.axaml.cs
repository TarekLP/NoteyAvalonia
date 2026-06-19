using Avalonia.Controls;
using Avalonia.Input;
using NoteToolAvalonia.Models;
using NoteToolAvalonia.ViewModels;

namespace NoteToolAvalonia.Views;

public partial class WelcomeView : UserControl
{
	public WelcomeView()
	{
		InitializeComponent();
		AddHandler(KeyDownEvent, OnKeyDown, handledEventsToo: true);
	}
	private void OnKeyDown(object? sender, KeyEventArgs e)
	{
		if (e.Key != Key.Enter) return;
		if (DataContext is not WelcomeViewModel vm) return;
		if (!vm.IsCreatingNote) return;

		if (vm.CreatorStep < 4)
			vm.CreatorNextCommand.Execute(null);
		else
			vm.CreateNoteCommand.Execute(null);

		e.Handled = true;

		Avalonia.Threading.Dispatcher.UIThread.Post(() =>
		{
			var box = this.FindControl<TextBox>($"WizardStep{vm.CreatorStep}Box");
			box?.Focus();
		}, Avalonia.Threading.DispatcherPriority.Input);
	}

	private void NoteCard_Tapped(object? sender, TappedEventArgs e)
	{
		if (sender is Border b && b.DataContext is NoteCard card &&
			DataContext is WelcomeViewModel vm)
		{
			vm.OpenNoteCommand.Execute(card);
		}
	}
}