using Avalonia.Controls;
using Avalonia.Input;
using NoteToolAvalonia.Models;
using NoteToolAvalonia.ViewModels;

namespace NoteToolAvalonia.Views;

public partial class BoardView : UserControl
{
	private static NoteCard? _draggedCard;
	private bool _isDragging;

	public BoardView()
	{
		InitializeComponent();
	}

	private async void NoteCard_PointerPressed(object? sender, PointerPressedEventArgs e)
	{
		if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
			return;

		if (sender is Control control && control.DataContext is NoteCard card)
		{
			_isDragging = false;
			_draggedCard = card;

			// Just pass an empty DataTransfer — we track the card via the field
			var result = await DragDrop.DoDragDropAsync(e, new DataTransfer(), DragDropEffects.Move);

			_isDragging = result == DragDropEffects.Move;
		}
	}

	private void Column_DragOver(object? sender, DragEventArgs e)
	{
		// Check our field instead of the data transfer object
		e.DragEffects = _draggedCard != null
			? DragDropEffects.Move
			: DragDropEffects.None;
	}

	private void Column_Drop(object? sender, DragEventArgs e)
	{
		if (_draggedCard == null) return;

		if (sender is Control control &&
			control.DataContext is BoardColumn targetColumn &&
			DataContext is BoardViewModel viewModel)
		{
			viewModel.MoveCardToColumn(_draggedCard, targetColumn);
			_isDragging = true;
		}

		_draggedCard = null;
	}

	private void NoteCard_Tapped(object? sender, TappedEventArgs e)
	{
		if (_isDragging)
		{
			_isDragging = false;
			return;
		}

		if (sender is Control control &&
			control.DataContext is NoteCard card &&
			DataContext is BoardViewModel viewModel)
		{
			viewModel.OpenCardCommand.Execute(card);
		}
	}
}