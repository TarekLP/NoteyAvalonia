using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using NoteToolAvalonia.Models;
using NoteToolAvalonia.ViewModels;
using System;

namespace NoteToolAvalonia.Views;

public partial class BoardView : UserControl
{
	private Point _pressPosition;
	private bool _isPressed;
	private NoteCard? _pendingDragCard;

	public BoardView()
	{
		InitializeComponent();
	}

	private void NoteCard_PointerPressed(object? sender, PointerPressedEventArgs e)
	{
		if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

		if (sender is Control control && control.DataContext is NoteCard card)
		{
			_isPressed = true;
			_pressPosition = e.GetPosition(this);
			_pendingDragCard = card;
		}
	}

	private void NoteCard_PointerMoved(object? sender, PointerEventArgs e)
	{
		if (!_isPressed || _pendingDragCard == null) return;

		var currentPos = e.GetPosition(this);
		var distance = Math.Sqrt(
			Math.Pow(currentPos.X - _pressPosition.X, 2) +
			Math.Pow(currentPos.Y - _pressPosition.Y, 2));

		if (distance > 5)
		{
			_isPressed = false;
			var draggedCard = _pendingDragCard;
			_pendingDragCard = null;

			// ✅ WORKAROUND: Use DataObject + synchronous DoDragDrop
			var data = new DataObject();
			data.Set("NoteCard", draggedCard);

			DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
		}
	}

	private void NoteCard_PointerReleased(object? sender, PointerReleasedEventArgs e)
	{
		_isPressed = false;
		_pendingDragCard = null;
	}

	private void NoteCard_DoubleTapped(object? sender, TappedEventArgs e)
	{
		if (sender is Control control &&
			control.DataContext is NoteCard card &&
			DataContext is BoardViewModel viewModel)
		{
			_isPressed = false;
			_pendingDragCard = null;
			viewModel.OpenCardCommand.Execute(card);
			e.Handled = true;
		}
	}

	private void Column_DragOver(object? sender, DragEventArgs e)
	{
		if (e.Data.Contains("NoteCard"))
			e.DragEffects = DragDropEffects.Move;
		else
			e.DragEffects = DragDropEffects.None;
	}

	private void Column_Drop(object? sender, DragEventArgs e)
	{
		if (!e.Data.Contains("NoteCard") ||
			e.Data.Get("NoteCard") is not NoteCard draggedCard)
			return;

		if (sender is Control control &&
			control.DataContext is BoardColumn targetColumn &&
			DataContext is BoardViewModel viewModel)
		{
			viewModel.MoveCardToColumn(draggedCard, targetColumn);
		}
	}
}