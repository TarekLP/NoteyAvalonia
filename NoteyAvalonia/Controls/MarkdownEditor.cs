using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace NoteToolAvalonia.Controls;

public class MarkdownEditor : TemplatedControl
{
	public static readonly StyledProperty<string?> MarkdownProperty =
		AvaloniaProperty.Register<MarkdownEditor, string?>(nameof(Markdown));

	public string? Markdown
	{
		get => GetValue(MarkdownProperty);
		set => SetValue(MarkdownProperty, value);
	}

	private TextBox? _editorInput;

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);
		_editorInput = e.NameScope.Find<TextBox>("PART_EditorInput");
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