using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using NoteToolAvalonia.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace NoteToolAvalonia.Views;

public class BoolToTextWrappingConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is bool b)
		{
			return b ? Avalonia.Media.TextWrapping.Wrap : Avalonia.Media.TextWrapping.NoWrap;
		}
		return Avalonia.Media.TextWrapping.Wrap;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}

public partial class NoteEditorView : UserControl
{
	private TextBox? _editorTextBox;
	private Border? _previewPane;
	private Button? _previewToggleButton;
	private TextBlock? _lineNumbersPanel;

	// Smart wrapping buttons
	private Button? _boldButton;
	private Button? _italicButton;
	private Button? _strikethroughButton;
	private Button? _codeButton;
	private Button? _heading1Button;
	private Button? _heading2Button;
	private Button? _heading3Button;
	private Button? _bulletListButton;
	private Button? _numberedListButton;
	private Button? _checklistButton;
	private Button? _quoteButton;

	public NoteEditorView()
	{
		InitializeComponent();
	}

	private void InitializeComponent()
	{
		AvaloniaXamlLoader.Load(this);
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree(e);

		_editorTextBox = this.FindControl<TextBox>("EditorTextBox");
		_previewPane = this.FindControl<Border>("PreviewPane");
		_previewToggleButton = this.FindControl<Button>("PreviewToggleButton");
		_lineNumbersPanel = this.FindControl<TextBlock>("LineNumbersPanel");

		// Find smart wrapping buttons
		_boldButton = this.FindControl<Button>("BoldButton");
		_italicButton = this.FindControl<Button>("ItalicButton");
		_strikethroughButton = this.FindControl<Button>("StrikethroughButton");
		_codeButton = this.FindControl<Button>("CodeButton");
		_heading1Button = this.FindControl<Button>("Heading1Button");
		_heading2Button = this.FindControl<Button>("Heading2Button");
		_heading3Button = this.FindControl<Button>("Heading3Button");
		_bulletListButton = this.FindControl<Button>("BulletListButton");
		_numberedListButton = this.FindControl<Button>("NumberedListButton");
		_checklistButton = this.FindControl<Button>("ChecklistButton");
		_quoteButton = this.FindControl<Button>("QuoteButton");

		if (_previewToggleButton != null)
		{
			_previewToggleButton.Click += PreviewToggle_Click;
		}

		// Wire up smart wrapping buttons
		if (_boldButton != null) _boldButton.Click += (s, e) => WrapSelectedText("**", "**");
		if (_italicButton != null) _italicButton.Click += (s, e) => WrapSelectedText("*", "*");
		if (_strikethroughButton != null) _strikethroughButton.Click += (s, e) => WrapSelectedText("~~", "~~");
		if (_codeButton != null) _codeButton.Click += (s, e) => WrapSelectedText("`", "`");
		if (_heading1Button != null) _heading1Button.Click += (s, e) => PrependToLine("# ");
		if (_heading2Button != null) _heading2Button.Click += (s, e) => PrependToLine("## ");
		if (_heading3Button != null) _heading3Button.Click += (s, e) => PrependToLine("### ");
		if (_bulletListButton != null) _bulletListButton.Click += (s, e) => PrependToLine("- ");
		if (_numberedListButton != null) _numberedListButton.Click += (s, e) => PrependToLine("1. ");
		if (_checklistButton != null) _checklistButton.Click += (s, e) => PrependToLine("- [ ] ");
		if (_quoteButton != null) _quoteButton.Click += (s, e) => PrependToLine("> ");

		// Wire up toolbar buttons for insert-at-cursor
		foreach (var button in this.GetVisualDescendants().OfType<Button>())
		{
			var toolTip = button.GetValue(ToolTip.TipProperty)?.ToString() ?? "";
			if (string.IsNullOrEmpty(toolTip) || toolTip.Contains("Back") || toolTip.Contains("Save"))
				continue;

			// Skip buttons we already handled
			if (button == _boldButton || button == _italicButton || button == _strikethroughButton ||
				button == _codeButton || button == _heading1Button || button == _heading2Button ||
				button == _heading3Button || button == _bulletListButton || button == _numberedListButton ||
				button == _checklistButton || button == _quoteButton || button == _previewToggleButton)
				continue;

			button.Click += (s, e) => OnToolbarButtonClick((Button)s!);
		}

		AddHandler(KeyDownEvent, OnKeyDown, handledEventsToo: true);
		UpdatePreviewVisibility();
		UpdateLineNumbers();

		// Listen for content changes to update line numbers
		if (_editorTextBox != null)
		{
			_editorTextBox.TextChanged += EditorTextBox_TextChanged;
		}
	}

	private void EditorTextBox_TextChanged(object? sender, TextChangedEventArgs e)
	{
		UpdateLineNumbers();
	}

	private void UpdateLineNumbers()
	{
		if (_lineNumbersPanel == null || _editorTextBox == null) return;

		var lineCount = _editorTextBox.Text?.Count(c => c == '\n') + 1 ?? 1;
		var lineNumbers = string.Join(Environment.NewLine, Enumerable.Range(1, lineCount));
		_lineNumbersPanel.Text = lineNumbers;
	}

	/// <summary>
	/// Wrap selected text with before and after markers.
	/// If no text is selected, inserts the markers at cursor.
	/// </summary>
	private void WrapSelectedText(string before, string after)
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
			// No selection: insert markers at cursor
			var newText = text.Substring(0, cursorPos) + before + after + text.Substring(cursorPos);
			_editorTextBox.Text = newText;
			_editorTextBox.CaretIndex = cursorPos + before.Length;
		}
	}

	/// <summary>
	/// Prepend text to the line containing the cursor.
	/// If text is selected, prepends to each line in the selection.
	/// </summary>
	private void PrependToLine(string prefix)
	{
		if (_editorTextBox == null) return;

		var text = _editorTextBox.Text ?? "";
		var cursorPos = _editorTextBox.CaretIndex;
		var selectedLength = _editorTextBox.SelectedText?.Length ?? 0;

		// Find the start of the current line
		var lineStart = text.LastIndexOf('\n', cursorPos - 1) + 1;
		if (lineStart < 0) lineStart = 0;

		// If text is selected, apply to each line in selection
		if (selectedLength > 0)
		{
			var selectionStart = _editorTextBox.SelectionStart;
			var selectionEnd = selectionStart + selectedLength;

			// Find start of first line in selection
			var firstLineStart = text.LastIndexOf('\n', selectionStart - 1) + 1;
			if (firstLineStart < 0) firstLineStart = 0;

			// Find end of last line in selection
			var lastLineEnd = text.IndexOf('\n', selectionEnd);
			if (lastLineEnd < 0) lastLineEnd = text.Length;

			var selectedLines = text.Substring(firstLineStart, lastLineEnd - firstLineStart);
			var lines = selectedLines.Split('\n');
			var modifiedLines = lines.Select(line => prefix + line);
			var newSelected = string.Join("\n", modifiedLines);

			var newText = text.Substring(0, firstLineStart) + newSelected + text.Substring(lastLineEnd);
			_editorTextBox.Text = newText;
			_editorTextBox.CaretIndex = firstLineStart + newSelected.Length;
		}
		else
		{
			// Just the current line
			var currentLine = text.Substring(lineStart, (_editorTextBox.Text?.IndexOf('\n', lineStart) ?? _editorTextBox.Text?.Length ?? 0) - lineStart);
			var newLine = prefix + currentLine;
			var newText = text.Substring(0, lineStart) + newLine + text.Substring(lineStart + currentLine.Length);
			_editorTextBox.Text = newText;
			_editorTextBox.CaretIndex = cursorPos + prefix.Length;
		}
	}

	private void OnKeyDown(object? sender, KeyEventArgs e)
	{
		if (DataContext is not NoteEditorViewModel vm) return;

		// Ctrl+Z for Undo
		if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.Z)
		{
			vm.UndoCommand.Execute(null);
			e.Handled = true;
		}
		// Ctrl+Y for Redo
		else if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.Y)
		{
			vm.RedoCommand.Execute(null);
			e.Handled = true;
		}
		// Ctrl+B for Bold
		else if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.B)
		{
			WrapSelectedText("**", "**");
			e.Handled = true;
		}
		// Ctrl+I for Italic
		else if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.I)
		{
			WrapSelectedText("*", "*");
			e.Handled = true;
		}
		// Ctrl+H for Find & Replace
		else if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.H)
		{
			vm.ShowFindReplacePanelCommand.Execute(null);
			e.Handled = true;
		}
		// Ctrl+F for Find (if we had a simple find-only mode, but H opens both)
		else if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.F)
		{
			vm.ShowFindReplacePanelCommand.Execute(null);
			e.Handled = true;
		}
		// Ctrl+Shift+W for Word Wrap
		else if (e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift) && e.Key == Key.W)
		{
			vm.ToggleWordWrapCommand.Execute(null);
			e.Handled = true;
		}
		// Ctrl+Shift+P for Preview toggle
		else if (e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift) && e.Key == Key.P)
		{
			vm.IsPreviewVisible = !vm.IsPreviewVisible;
			UpdatePreviewVisibility();
			e.Handled = true;
		}
		// Ctrl+Shift+L for Line Numbers
		else if (e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift) && e.Key == Key.L)
		{
			vm.ToggleLineNumbersCommand.Execute(null);
			e.Handled = true;
		}
	}

	private void OnToolbarButtonClick(Button button)
	{
		if (_editorTextBox == null) return;

		var toolTip = button.GetValue(ToolTip.TipProperty)?.ToString() ?? "";

		// Code Block
		if (toolTip.Contains("Code Block"))
			AppendText("\n```\ncode block\n```\n");
		// Link
		else if (toolTip.Contains("Link"))
			AppendText("[link text](url)");
		// Image
		else if (toolTip.Contains("Image"))
			AppendText("![alt text](image-url)");
		// Table
		else if (toolTip.Contains("Table"))
			AppendText("\n| Header 1 | Header 2 |\n| -------- | -------- |\n| Cell 1   | Cell 2   |");
		// Horizontal Rule
		else if (toolTip.Contains("Horizontal"))
			AppendText("\n---\n");

		_editorTextBox?.Focus();
	}

	private void AppendText(string text)
	{
		if (_editorTextBox == null) return;

		var cursorPos = _editorTextBox.CaretIndex;
		var currentText = _editorTextBox.Text ?? "";
		_editorTextBox.Text = currentText.Substring(0, cursorPos) + text + currentText.Substring(cursorPos);
		_editorTextBox.CaretIndex = cursorPos + text.Length;
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
