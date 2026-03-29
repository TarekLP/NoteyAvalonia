using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using NoteToolAvalonia.ViewModels;
using System;
using System.Linq;

namespace NoteToolAvalonia.Views;

public partial class NoteEditorView : UserControl
{
	private TextBox? _editorTextBox;
	private Border? _previewPane;
	private Button? _previewToggleButton;

	public NoteEditorView()
	{
		InitializeComponent();
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree(e);

		_editorTextBox = this.FindControl<TextBox>("EditorTextBox");
		_previewPane = this.FindControl<Border>("PreviewPane");
		_previewToggleButton = this.FindControl<Button>("PreviewToggleButton");

		if (_previewToggleButton != null)
		{
			_previewToggleButton.Click += PreviewToggle_Click;
		}

		// Wire up toolbar buttons - collect all buttons except specific ones
		foreach (var button in this.GetVisualDescendants().OfType<Button>())
		{
			if (button == _previewToggleButton) continue;

			var toolTip = button.GetValue(ToolTip.TipProperty)?.ToString() ?? "";
			if (string.IsNullOrEmpty(toolTip) || toolTip.Contains("Back") || toolTip.Contains("Save"))
				continue;

			button.Click += (s, e) => OnToolbarButtonClick((Button)s!);
		}

		AddHandler(KeyDownEvent, OnKeyDown, handledEventsToo: true);
		UpdatePreviewVisibility();
	}

	private void OnKeyDown(object? sender, KeyEventArgs e)
	{
		if (DataContext is not NoteEditorViewModel vm) return;

		// Ctrl+B for Bold
		if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.B)
		{
			InsertMarkdown("**", "**");
			e.Handled = true;
		}
		// Ctrl+I for Italic
		else if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.I)
		{
			InsertMarkdown("_", "_");
			e.Handled = true;
		}
		// Ctrl+Shift+P for Preview toggle
		else if (e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift) && e.Key == Key.P)
		{
			vm.IsPreviewVisible = !vm.IsPreviewVisible;
			UpdatePreviewVisibility();
			e.Handled = true;
		}
	}

	private void OnToolbarButtonClick(Button button)
	{
		if (_editorTextBox == null) return;

		var toolTip = button.GetValue(ToolTip.TipProperty)?.ToString() ?? "";

		// Formatting
		if (toolTip.Contains("Bold"))
			InsertMarkdown("**", "**");
		else if (toolTip.Contains("Italic"))
			InsertMarkdown("_", "_");
		else if (toolTip.Contains("Strikethrough"))
			InsertMarkdown("~~", "~~");

		// Headings
		else if (toolTip.Contains("Heading 1"))
			InsertMarkdown("# ", "");
		else if (toolTip.Contains("Heading 2"))
			InsertMarkdown("## ", "");
		else if (toolTip.Contains("Heading 3"))
			InsertMarkdown("### ", "");

		// Lists
		else if (toolTip.Contains("Bullet List"))
			InsertMarkdown("- ", "");
		else if (toolTip.Contains("Numbered List"))
			InsertMarkdown("1. ", "");
		else if (toolTip.Contains("Checklist"))
			InsertMarkdown("- [ ] ", "");

		// Code and Quote
		else if (toolTip.Contains("Code Block"))
			InsertMarkdown("```\n", "\n```");
		else if (toolTip.Contains("Quote"))
			InsertMarkdown("> ", "");

		_editorTextBox?.Focus();
	}

	private void InsertMarkdown(string before, string after)
	{
		if (_editorTextBox == null) return;

		var cursorPos = _editorTextBox.CaretIndex;
		var text = _editorTextBox.Text ?? "";
		var selectedLength = _editorTextBox.SelectedText?.Length ?? 0;

		if (selectedLength > 0)
		{
			// Wrap selection
			var start = _editorTextBox.SelectionStart;
			var selectedText = _editorTextBox.SelectedText;
			var newText = text.Substring(0, start) + before + selectedText + after + text.Substring(start + selectedLength);
			_editorTextBox.Text = newText;
			_editorTextBox.CaretIndex = start + before.Length + selectedLength;
		}
		else
		{
			// Insert at cursor
			var newText = text.Substring(0, cursorPos) + before + after + text.Substring(cursorPos);
			_editorTextBox.Text = newText;
			_editorTextBox.CaretIndex = cursorPos + before.Length;
		}
	}

	private void PreviewToggle_Click(object? sender, RoutedEventArgs e)
	{
		if (DataContext is NoteEditorViewModel vm)
		{
			vm.IsPreviewVisible = !vm.IsPreviewVisible;
			UpdatePreviewVisibility();
		}
	}

	private void UpdatePreviewVisibility()
	{
		if (DataContext is not NoteEditorViewModel vm || _previewPane == null) return;

		_previewPane.IsVisible = vm.IsPreviewVisible;
	}
}