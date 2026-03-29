using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using NoteToolAvalonia.Models;
using NoteToolAvalonia.ViewModels;
using System;

namespace NoteToolAvalonia.Views;

public partial class BoardView : UserControl
{
	private Point _pressPosition;
	private bool _isPressed;
	private NoteCard? _pendingDragCard;
	private Popup? _dragVisualPopup;
	private Border? _dragVisualContent;

	public BoardView()
	{
		InitializeComponent();
		AddHandler(DragDrop.DragOverEvent, Column_DragOver, handledEventsToo: true);
		AddHandler(DragDrop.DropEvent, Column_Drop, handledEventsToo: true);
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

			// Create and show drag visual
			ShowDragVisual(draggedCard, e);

			// ✅ WORKAROUND: Use DataObject + synchronous DoDragDrop
			var data = new DataObject();
			data.Set("NoteCard", draggedCard);

			DragDrop.DoDragDrop(e, data, DragDropEffects.Move);

			// Hide drag visual
			HideDragVisual();
		}
	}

	private void NoteCard_PointerReleased(object? sender, PointerReleasedEventArgs e)
	{
		_isPressed = false;
		_pendingDragCard = null;
		HideDragVisual();
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

	private void ShowDragVisual(NoteCard card, PointerEventArgs e)
	{
		// Create drag visual content
		_dragVisualContent = new Border
		{
			Background = new SolidColorBrush(Color.Parse("#3A3A3D")),
			CornerRadius = new CornerRadius(6),
			Padding = new Thickness(8),
			Width = 240,
			MaxHeight = 100,
			Opacity = 0.9,
			Child = new StackPanel
			{
				Children =
				{
					new TextBlock
					{
						Text = card.Content,
						Foreground = new SolidColorBrush(Color.Parse("#DDD")),
						TextWrapping = TextWrapping.Wrap,
						MaxLines = 2,
						TextTrimming = TextTrimming.CharacterEllipsis
					}
				}
			}
		};

		// Create popup for drag visual
		_dragVisualPopup = new Popup
		{
			Child = _dragVisualContent,
			IsOpen = true,
			Placement = PlacementMode.AnchorAndGravity
		};

		// Position at cursor
		UpdateDragVisualPosition(e);

		// Subscribe to pointer move to follow cursor
		PointerMoved += DragVisual_PointerMoved;
	}

	private void HideDragVisual()
	{
		PointerMoved -= DragVisual_PointerMoved;

		if (_dragVisualPopup != null)
		{
			_dragVisualPopup.IsOpen = false;
			_dragVisualPopup = null;
		}

		_dragVisualContent = null;
	}

	private void DragVisual_PointerMoved(object? sender, PointerEventArgs e)
	{
		if (_dragVisualPopup != null)
		{
			UpdateDragVisualPosition(e);
		}
	}

	private void UpdateDragVisualPosition(PointerEventArgs e)
	{
		if (_dragVisualPopup != null)
		{
			var screenPos = this.PointToScreen(e.GetPosition(this));
			_dragVisualPopup.HorizontalOffset = screenPos.X + 10;
			_dragVisualPopup.VerticalOffset = screenPos.Y + 10;
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

		// Find the column border that was dropped on
		var source = e.Source as Control;
		Border? columnBorder = null;
		var current = source;

		while (current != null && columnBorder == null)
		{
			if (current is Border border && border.Name == "ColumnBorder")
			{
				columnBorder = border;
				break;
			}
			current = current.Parent as Control;
		}

		if (columnBorder != null &&
			columnBorder.DataContext is BoardColumn targetColumn &&
			DataContext is BoardViewModel viewModel)
		{
			viewModel.MoveCardToColumn(draggedCard, targetColumn);
		}
	}
}