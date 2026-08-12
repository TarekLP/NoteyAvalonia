using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace NoteToolAvalonia.Controls;

public class MarkdownEditor : TemplatedControl
{
	public static readonly StyledProperty<string?> MarkdownProperty =
		AvaloniaProperty.Register<MarkdownEditor, string?>(nameof(Markdown));

	public static readonly StyledProperty<bool> ShowLineNumbersProperty =
		AvaloniaProperty.Register<MarkdownEditor, bool>(nameof(ShowLineNumbers), true);

	public static readonly StyledProperty<bool> WordWrapEnabledProperty =
		AvaloniaProperty.Register<MarkdownEditor, bool>(nameof(WordWrapEnabled), true);

	public string? Markdown
	{
		get => GetValue(MarkdownProperty);
		set => SetValue(MarkdownProperty, value);
	}

	public bool ShowLineNumbers
	{
		get => GetValue(ShowLineNumbersProperty);
		set => SetValue(ShowLineNumbersProperty, value);
	}

	public bool WordWrapEnabled
	{
		get => GetValue(WordWrapEnabledProperty);
		set => SetValue(WordWrapEnabledProperty, value);
	}

	private TextBox? _editorInput;
	private TextBlock? _lineNumbers;

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);
		_editorInput = e.NameScope.Find<TextBox>("PART_EditorInput");
		_lineNumbers = e.NameScope.Find<TextBlock>("PART_LineNumbers");

		if (_editorInput != null)
			_editorInput.TextChanged += OnEditorTextChanged;

		UpdateLineNumbers();
	}

	private void OnEditorTextChanged(object? sender, TextChangedEventArgs e) => UpdateLineNumbers();

	private void UpdateLineNumbers()
	{
		if (_lineNumbers == null || _editorInput == null) return;

		var text = _editorInput.Text ?? string.Empty;
		var count = text.Length == 0 ? 1 : text.Split('\n').Length;
		_lineNumbers.Text = string.Join("\n", Enumerable.Range(1, count));
	}

	public void WrapSelection(string open, string close)
	{
		if (_editorInput == null) return;
		var start = _editorInput.SelectionStart;
		var end = _editorInput.SelectionEnd;
		var text = _editorInput.Text ?? "";

		var selected = text.Substring(start, end - start);
		var newText = text.Remove(start, end - start).Insert(start, $"{open}{selected}{close}");

		Markdown = newText;
		_editorInput.SelectionStart = start + open.Length;
		_editorInput.SelectionEnd = start + open.Length + selected.Length;
	}

	public void PrependLine(string prefix)
	{
		if (_editorInput == null) return;
		var caret = _editorInput.CaretIndex;
		var text = _editorInput.Text ?? "";

		int lineStart = text.LastIndexOf('\n', Math.Max(0, caret - 1)) + 1;
		Markdown = text.Insert(lineStart, prefix);
		_editorInput.CaretIndex = caret + prefix.Length;
	}

	public void InsertAtCursor(string textToInsert)
	{
		if (_editorInput == null) return;
		var caret = _editorInput.CaretIndex;
		var text = _editorInput.Text ?? "";

		Markdown = text.Insert(caret, textToInsert);
		_editorInput.CaretIndex = caret + textToInsert.Length;
	}
}