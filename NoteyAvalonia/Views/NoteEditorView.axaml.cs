using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NoteToolAvalonia.Models;
using NoteToolAvalonia.ViewModels;
using MarkdownUIComponent;

namespace NoteToolAvalonia.Views;

// ════════════════════════════════════════════════════════════
//  NOTE EDITOR VIEW
//  Single-pane editor backed by MarkdownUIComponent's TextBox.
//  Toolbar / line numbers / status bar / keybinds preserved.
// ════════════════════════════════════════════════════════════
public partial class NoteEditorView : UserControl
{
    private TextBox?  _editor;        // inner TextBox of CustomMarkdownEditor
    private TextBlock? _lineNumbers;
    private bool      _updatingFromVm;

    private Button? _boldBtn, _italicBtn, _strikeBtn, _codeBtn;
    private Button? _h1Btn, _h2Btn, _h3Btn;
    private Button? _bulletBtn, _numberedBtn, _checklistBtn, _quoteBtn;
    private Button? _tableBtn, _hrBtn;

    public NoteEditorView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _editor      = FindInnerTextBox();
        _lineNumbers = this.FindControl<TextBlock>("LineNumbersPanel");

        if (_editor == null) return;

        _editor.TextChanged += (_, _) =>
        {
            if (_updatingFromVm) return;
            if (DataContext is NoteEditorViewModel v)
                // WYSIWYG: read Markdown (with markers) from the control, not
                // the raw TextBox text (which has markers stripped).
                v.NoteContent = _mdEditor?.Markdown ?? _editor.Text ?? string.Empty;
            UpdateLineNumbers();
        };

        AddHandler(KeyDownEvent, OnKeyDown, handledEventsToo: true);
        FindToolbarButtons();
        WireToolbarButtons();

        SyncContentToEditor();

        if (DataContext is NoteEditorViewModel vm)
        {
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(NoteEditorViewModel.NoteContent) && !_updatingFromVm)
                    SyncContentToEditor();
            };
        }
    }

    private CustomMarkdownEditor? _mdEditor;

    // CustomMarkdownEditor → Grid → col 0 = EditorInput TextBox
    private TextBox? FindInnerTextBox()
    {
        var host = this.FindControl<MarkdownEditorOnly>("MarkdownEditorHost");
        if (host is null) return null;
        // The named CustomMarkdownEditor inside the host (set in MarkdownEditorOnly.axaml).
        _mdEditor = host.GetVisualDescendants()
                        .OfType<CustomMarkdownEditor>()
                        .FirstOrDefault();
        if (_mdEditor is null) return null;
        return _mdEditor.GetVisualDescendants()
                        .OfType<TextBox>()
                        .FirstOrDefault(t => t.Name == "EditorInput");
    }

    private void SyncContentToEditor()
    {
        if (_editor == null) return;
        if (DataContext is not NoteEditorViewModel vm) return;

        _updatingFromVm = true;
        // WYSIWYG: push Markdown to the control; the control re-renders display + overlay.
        if (_mdEditor is not null) _mdEditor.Markdown = vm.NoteContent ?? string.Empty;
        else _editor.Text = vm.NoteContent ?? string.Empty;
        _updatingFromVm = false;

        UpdateLineNumbers();

        Dispatcher.UIThread.Post(() => _editor.Focus(), DispatcherPriority.Input);
    }

    // ── Keyboard shortcuts ──────────────────────────────────

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not NoteEditorViewModel vm) return;

        if (e.KeyModifiers == KeyModifiers.Control)
        {
            switch (e.Key)
            {
                case Key.S: vm.SaveCommand.Execute(null);                 e.Handled = true; break;
                case Key.B: WrapSelection("**", "**");                   e.Handled = true; break;
                case Key.I: WrapSelection("*",  "*");                    e.Handled = true; break;
                case Key.H:
                case Key.F: vm.ShowFindReplacePanelCommand.Execute(null); e.Handled = true; break;
            }
        }
        else if (e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift))
        {
            switch (e.Key)
            {
                case Key.W: vm.ToggleWordWrapCommand.Execute(null);    e.Handled = true; break;
                case Key.L: vm.ToggleLineNumbersCommand.Execute(null); e.Handled = true; break;
            }
        }
    }

    // ── Toolbar ─────────────────────────────────────────────

    private void FindToolbarButtons()
    {
        _boldBtn      = this.FindControl<Button>("BoldButton");
        _italicBtn    = this.FindControl<Button>("ItalicButton");
        _strikeBtn    = this.FindControl<Button>("StrikeButton");
        _codeBtn      = this.FindControl<Button>("CodeButton");
        _h1Btn        = this.FindControl<Button>("H1Button");
        _h2Btn        = this.FindControl<Button>("H2Button");
        _h3Btn        = this.FindControl<Button>("H3Button");
        _bulletBtn    = this.FindControl<Button>("BulletButton");
        _numberedBtn  = this.FindControl<Button>("NumberedButton");
        _checklistBtn = this.FindControl<Button>("ChecklistButton");
        _quoteBtn     = this.FindControl<Button>("QuoteButton");
        _tableBtn     = this.FindControl<Button>("TableButton");
        _hrBtn        = this.FindControl<Button>("HRButton");
    }

    private void WireToolbarButtons()
    {
        _boldBtn?.Click      += (_, _) => WrapSelection("**", "**");
        _italicBtn?.Click    += (_, _) => WrapSelection("*",  "*");
        _strikeBtn?.Click    += (_, _) => WrapSelection("~~", "~~");
        _codeBtn?.Click      += (_, _) => WrapSelection("`",  "`");
        _h1Btn?.Click        += (_, _) => PrependLine("# ");
        _h2Btn?.Click        += (_, _) => PrependLine("## ");
        _h3Btn?.Click        += (_, _) => PrependLine("### ");
        _bulletBtn?.Click    += (_, _) => PrependLine("- ");
        _numberedBtn?.Click  += (_, _) => PrependLine("1. ");
        _checklistBtn?.Click += (_, _) => PrependLine("- [ ] ");
        _quoteBtn?.Click     += (_, _) => PrependLine("> ");
        _tableBtn?.Click     += (_, _) => InsertAtCursor("\n| Header 1 | Header 2 |\n| --- | --- |\n| Cell | Cell |\n");
        _hrBtn?.Click        += (_, _) => InsertAtCursor("\n---\n");
        _editor?.Focus();
    }

    // ── Text helpers (operate on the inner TextBox) ─────────

    private int GetCaret()
    {
        if (_editor is null) return 0;
        return _editor.CaretIndex;
    }

    private void SetCaret(int pos)
    {
        if (_editor is null) return;
        _editor.CaretIndex = Math.Clamp(pos, 0, (_editor.Text ?? string.Empty).Length);
    }

    // ponytail: edit on display text, then push to _mdEditor.Markdown so the
    // WYSIWYG overlay re-renders. The shipped MarkdownUIComponent.dll only
    // exposes the Markdown property — the wrap/prepend/insert helpers are not
    // public. Re-introduce them with display→raw mapping once the control grows
    // a public API; until then, raw markdown markers in user selection are
    // not preserved across a toolbar wrap (the overlay will show plain text).
    private void PushMarkdown(string newText)
    {
        if (_mdEditor is not null)
            _mdEditor.Markdown = newText;
        else if (_editor is not null)
            _editor.Text = newText;
    }

    private void WrapSelection(string open, string close)
    {
        if (_editor == null) return;
        var text = _editor.Text ?? string.Empty;
        var caret = _editor.CaretIndex;
        var selStart = _editor.SelectionStart;
        var selEnd   = _editor.SelectionEnd;

        if (selStart < selEnd)
        {
            var selected = text.Substring(selStart, selEnd - selStart);
            var replaced = open + selected + close;
            PushMarkdown(text.Substring(0, selStart) + replaced + text.Substring(selEnd));
            SetCaret(selStart + replaced.Length);
        }
        else
        {
            var inserted = open + close;
            PushMarkdown(text.Substring(0, caret) + inserted + text.Substring(caret));
            SetCaret(caret + open.Length);
        }
        _editor.Focus();
    }

    private void PrependLine(string prefix)
    {
        if (_editor == null) return;
        var text = _editor.Text ?? string.Empty;
        var caret = _editor.CaretIndex;
        // Find start of current line
        var lineStart = text.LastIndexOf('\n', Math.Max(0, caret - 1)) + 1;
        PushMarkdown(text.Substring(0, lineStart) + prefix + text.Substring(lineStart));
        SetCaret(lineStart + prefix.Length);
        _editor.Focus();
    }

    private void InsertAtCursor(string text)
    {
        if (_editor == null) return;
        var cur = _editor.Text ?? string.Empty;
        var caret = _editor.CaretIndex;
        PushMarkdown(cur.Substring(0, caret) + text + cur.Substring(caret));
        SetCaret(caret + text.Length);
        _editor.Focus();
    }

    private void UpdateLineNumbers()
    {
        if (_lineNumbers == null || _editor == null) return;
        var text = _editor.Text ?? string.Empty;
        var lineCount = text.Length == 0 ? 1 : text.Split('\n').Length;
        _lineNumbers.Text = string.Join(Environment.NewLine, Enumerable.Range(1, lineCount));
    }

    // ── References panel ────────────────────────────────────

    private void ReferencedNote_Tapped(object? sender, TappedEventArgs e)
    {
        if (sender is TextBlock tb && tb.DataContext is NoteCard card &&
            DataContext is NoteEditorViewModel vm)
            vm.OpenReferencedNoteCommand.Execute(card);
    }
}
