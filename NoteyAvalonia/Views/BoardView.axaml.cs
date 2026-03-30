using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Interactivity;
using NoteToolAvalonia.Models;
using NoteToolAvalonia.ViewModels;
using System;
using System.Linq;

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

	// ========================
	// CONTEXT MENU HELPERS
	// ========================

	private NoteCard? GetCardFromSender(object? sender)
	{
		if (sender is MenuItem menuItem)
		{
			return menuItem.DataContext as NoteCard;
		}
		return null;
	}

	private void SetPriority(object? sender, NotePriority priority)
	{
		if (GetCardFromSender(sender) is not NoteCard card) return;
		if (DataContext is not BoardViewModel vm) return;

		card.Priority = priority;
		card.LastModified = DateTime.Now;
		vm.Save(); // Use the Save method from the ViewModel
	}

	private void Priority_None_Click(object? sender, RoutedEventArgs e)
		=> SetPriority(sender, NotePriority.None);

	private void Priority_Low_Click(object? sender, RoutedEventArgs e)
		=> SetPriority(sender, NotePriority.Low);

	private void Priority_Medium_Click(object? sender, RoutedEventArgs e)
		=> SetPriority(sender, NotePriority.Medium);

	private void Priority_High_Click(object? sender, RoutedEventArgs e)
		=> SetPriority(sender, NotePriority.High);

	private void Priority_Critical_Click(object? sender, RoutedEventArgs e)
		=> SetPriority(sender, NotePriority.Critical);

	// ========================
	// DRAG & DROP LOGIC (unchanged)
	// ========================

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

			ShowDragVisual(draggedCard, e);

			var data = new DataObject();
			data.Set("NoteCard", draggedCard);

			DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
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
			viewModel.OpenCardCommand.Execute(card);
			e.Handled = true;
		}
	}

	private void ShowDragVisual(NoteCard card, PointerEventArgs e)
	{
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
						Text = card.Title,
						Foreground = new SolidColorBrush(Color.Parse("#DDD")),
						TextWrapping = TextWrapping.Wrap,
						MaxLines = 2,
						TextTrimming = TextTrimming.CharacterEllipsis
					}
				}
			}
		};

		_dragVisualPopup = new Popup
		{
			Child = _dragVisualContent,
			IsOpen = true,
			Placement = PlacementMode.AnchorAndGravity
		};

		UpdateDragVisualPosition(e);
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
			UpdateDragVisualPosition(e);
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
		e.DragEffects = e.Data.Contains("NoteCard")
			? DragDropEffects.Move
			: DragDropEffects.None;
	}

	private void Column_Drop(object? sender, DragEventArgs e)
	{
		if (!e.Data.Contains("NoteCard") ||
			e.Data.Get("NoteCard") is not NoteCard draggedCard)
			return;

		var current = e.Source as Control;
		while (current != null)
		{
			if (current is Border border && border.Name == "ColumnBorder" &&
				border.DataContext is BoardColumn targetColumn &&
				DataContext is BoardViewModel vm)
			{
				vm.MoveCardToColumn(draggedCard, targetColumn);
				return;
			}
			current = current.Parent as Control;
		}
	}
}