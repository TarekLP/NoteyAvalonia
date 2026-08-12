using System;
using Avalonia.Interactivity;
using Avalonia.Controls;
using Avalonia.Input;
using NoteToolAvalonia.Controls;
using NoteToolAvalonia.ViewModels;
using NoteToolAvalonia.Models;

namespace NoteToolAvalonia.Views;

public partial class NoteEditorView : UserControl
{
	private MarkdownEditor? _mdEditor;

	public NoteEditorView()
	{
		InitializeComponent();
		_mdEditor = this.FindControl<MarkdownEditor>("MarkdownEditorHost");
		WireToolbarButtons();
		AddHandler(InputElement.KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
	}

	private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
	{
		if (!e.KeyModifiers.HasFlag(KeyModifiers.Control)) return;
		if (e.Key == Key.Z || e.Key == Key.Y)
		{
			var vm = DataContext as NoteEditorViewModel;
			if (vm == null) return;
			if (e.Key == Key.Z) vm.UndoCommand.Execute(null);
			else                vm.RedoCommand.Execute(null);
			e.Handled = true;
		}
	}

	private void WireToolbarButtons()
	{
		if (_mdEditor == null) return;

		BoldButton.Click += (_, _) => _mdEditor.WrapSelection("**", "**");
		ItalicButton.Click += (_, _) => _mdEditor.WrapSelection("*", "*");
		StrikeButton.Click += (_, _) => _mdEditor.WrapSelection("~~", "~~");
		CodeButton.Click += (_, _) => _mdEditor.WrapSelection("`", "`");
		H1Button.Click += (_, _) => _mdEditor.PrependLine("# ");
		H2Button.Click += (_, _) => _mdEditor.PrependLine("## ");
		H3Button.Click += (_, _) => _mdEditor.PrependLine("### ");
		BulletButton.Click += (_, _) => _mdEditor.PrependLine("- ");
		NumberedButton.Click += (_, _) => _mdEditor.PrependLine("1. ");
		QuoteButton.Click += (_, _) => _mdEditor.PrependLine("> ");
		TableButton.Click += (_, _) => _mdEditor.InsertAtCursor("\n| Col | Col |\n|---|---|\n|   |   |\n");
		HrButton.Click += (_, _) => _mdEditor.InsertAtCursor("\n---\n");
	}

	public void ReferencedNote_Tapped(object? sender, PointerPressedEventArgs e)
	{
		if (sender is Control c && c.DataContext is NoteCard linkedNote)
		{
			(DataContext as NoteEditorViewModel)?.OpenReferencedNoteCommand.Execute(linkedNote);
		}
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
		{
			switch (e.Key)
			{
				case Key.B: _mdEditor?.WrapSelection("**", "**"); e.Handled = true; break;
				case Key.I: _mdEditor?.WrapSelection("*", "*"); e.Handled = true; break;
				case Key.S: (DataContext as NoteEditorViewModel)?.SaveCommand.Execute(null); e.Handled = true; break;
			}
		}
		base.OnKeyDown(e);
	}
}