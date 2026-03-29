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

			var result = await DragDrop.DoDragDropAsync(e, new DataTransfer(), DragDropEffects.Move);

			if (result != DragDropEffects.None)
			{
				_isDragging = true;
			}
		}
	}

	private void Column_DragOver(object? sender, DragEventArgs e)
	{
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

	private void NoteCard_DoubleTapped(object? sender, TappedEventArgs e)
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