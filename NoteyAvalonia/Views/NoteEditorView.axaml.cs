using System;
using System.Linq;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using NoteToolAvalonia.Models;
using NoteToolAvalonia.ViewModels;

namespace NoteToolAvalonia.Views;

// ── Converter: bool → TextWrapping ──────────────────────────
public class BoolToTextWrappingConverter : IValueConverter
{
    public object? Convert(object? v, Type t, object? p, CultureInfo c) =>
        v is true ? TextWrapping.Wrap : TextWrapping.NoWrap;
    public object? ConvertBack(object? v, Type t, object? p, CultureInfo c) =>
        throw new NotImplementedException();
}

// ════════════════════════════════════════════════════════════
//  MARKDOWN COLORIZING TRANSFORMER
//
//  Runs on every visible line inside AvaloniaEdit and applies
//  colour / weight changes so the markdown syntax looks styled
//  directly in the editor — no separate preview pane needed.
//
//  This class is self-contained: copy it + wire it to any
//  AvaloniaEdit TextEditor and it just works.
// ════════════════════════════════════════════════════════════
public class MarkdownColorizingTransformer : DocumentColorizingTransformer
{
    // Colours matching the app's dark theme
    private static readonly Color HeadingColor  = Color.Parse("#c792ea"); // soft purple
    private static readonly Color CodeColor     = Color.Parse("#89ddff"); // cyan
    private static readonly Color LinkColor     = Color.Parse("#82aaff"); // blue
    private static readonly Color QuoteColor    = Color.Parse("#546e7a"); // muted
    private static readonly Color CheckDoneColor= Color.Parse("#2ed573"); // green

    protected override void ColorizeLine(DocumentLine line)
    {
        var text = CurrentContext.Document.GetText(line.Offset, line.Length);
        if (text.Length == 0) return;

        // Headings: # / ## / ###
        if (text.StartsWith("###"))      ApplyLine(line, HeadingColor, FontWeight.Bold, 15);
        else if (text.StartsWith("##")) ApplyLine(line, HeadingColor, FontWeight.Bold, 18);
        else if (text.StartsWith("#"))  ApplyLine(line, HeadingColor, FontWeight.Bold, 22);

        // Blockquote
        else if (text.StartsWith("> ")) ApplyLine(line, QuoteColor, FontWeight.Normal, 0);

        // Completed checklist item
        else if (text.TrimStart().StartsWith("- [x]", StringComparison.OrdinalIgnoreCase))
            ApplyLine(line, CheckDoneColor, FontWeight.Normal, 0);

        // Inline spans: bold, italic, code, links
        ApplyInlineSpans(line, text);
    }

    private void ApplyLine(DocumentLine line, Color color, FontWeight weight, double extraSize)
    {
        ChangeLinePart(line.Offset, line.EndOffset, el =>
        {
            el.TextRunProperties.SetForegroundBrush(new SolidColorBrush(color));
            el.TextRunProperties.SetFontRenderingEmSize(((double)weight));
			if (extraSize > 0)
            {
                var tf = el.TextRunProperties.Typeface;
                el.TextRunProperties.SetTypeface(new Typeface(tf.FontFamily, tf.Style, weight));
            }
        });
    }

    private void ApplyInlineSpans(DocumentLine line, string text)
    {
        // Bold **…**
        ApplyPattern(line, text, "**", "**", FontWeight.Bold, null);
        // Italic *…*  (single star, avoid double)
        ApplyItalic(line, text);
        // Inline code `…`
        ApplyCode(line, text);
        // Links [text](url)
        ApplyLinks(line, text);
    }

    private void ApplyPattern(DocumentLine line, string text,
        string open, string close, FontWeight weight, Color? color)
    {
        int start = 0;
        while (true)
        {
            int o = text.IndexOf(open,  start, StringComparison.Ordinal);
            if (o < 0) break;
            int c = text.IndexOf(close, o + open.Length, StringComparison.Ordinal);
            if (c < 0) break;

            int absO = line.Offset + o;
            int absC = line.Offset + c + close.Length;

            ChangeLinePart(absO, absC, el =>
            {
                el.TextRunProperties.SetFontRenderingEmSize(((double)weight));
				if (color.HasValue)
                    el.TextRunProperties.SetForegroundBrush(new SolidColorBrush(color.Value));
            });
            start = c + close.Length;
        }
    }

    private void ApplyItalic(DocumentLine line, string text)
    {
        int start = 0;
        while (true)
        {
            int o = text.IndexOf('*', start);
            if (o < 0) break;
            // Skip ** bold markers
            if (o + 1 < text.Length && text[o + 1] == '*') { start = o + 2; continue; }
            int c = text.IndexOf('*', o + 1);
            if (c < 0) break;
            if (c + 1 < text.Length && text[c + 1] == '*') { start = c + 2; continue; }

            int absO = line.Offset + o;
            int absC = line.Offset + c + 1;
            ChangeLinePart(absO, absC, el =>
            {
                var tf = el.TextRunProperties.Typeface;
                el.TextRunProperties.SetTypeface(
                    new Typeface(tf.FontFamily, FontStyle.Italic, tf.Weight));
            });
            start = c + 1;
        }
    }

    private void ApplyCode(DocumentLine line, string text)
    {
        int start = 0;
        while (true)
        {
            int o = text.IndexOf('`', start);
            if (o < 0) break;
            int c = text.IndexOf('`', o + 1);
            if (c < 0) break;

            int absO = line.Offset + o;
            int absC = line.Offset + c + 1;
            ChangeLinePart(absO, absC, el =>
                el.TextRunProperties.SetForegroundBrush(new SolidColorBrush(CodeColor)));
            start = c + 1;
        }
    }

    private void ApplyLinks(DocumentLine line, string text)
    {
        int start = 0;
        while (true)
        {
            int ob = text.IndexOf('[', start);
            if (ob < 0) break;
            int cb = text.IndexOf(']', ob);
            if (cb < 0) break;
            if (cb + 1 >= text.Length || text[cb + 1] != '(') { start = cb + 1; continue; }
            int cp = text.IndexOf(')', cb + 2);
            if (cp < 0) break;

            int absO = line.Offset + ob;
            int absC = line.Offset + cp + 1;
            ChangeLinePart(absO, absC, el =>
            {
                el.TextRunProperties.SetForegroundBrush(new SolidColorBrush(LinkColor));
                el.TextRunProperties.SetTextDecorations(TextDecorations.Underline);
            });
            start = cp + 1;
        }
    }
}

// ════════════════════════════════════════════════════════════
//  NOTE EDITOR VIEW — code-behind
// ════════════════════════════════════════════════════════════
public partial class NoteEditorView : UserControl
{
    private TextEditor?  _editor;
    private TextBlock?   _lineNumbers;
    private bool         _updatingFromVm; // prevents feedback loops

    // Toolbar buttons wired in OnAttachedToVisualTree
    private Button? _boldBtn, _italicBtn, _strikeBtn, _codeBtn;
    private Button? _h1Btn, _h2Btn, _h3Btn;
    private Button? _bulletBtn, _numberedBtn, _checklistBtn, _quoteBtn;
    private Button? _tableBtn, _hrBtn;

    public NoteEditorView() => InitializeComponent();

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _editor      = this.FindControl<TextEditor>("MarkdownEditor");
        _lineNumbers = this.FindControl<TextBlock>("LineNumbersPanel");

        if (_editor == null) return;

        // Attach the live-markdown colorizer
        _editor.TextArea.TextView.LineTransformers.Add(new MarkdownColorizingTransformer());

        // Sync editor → ViewModel
        _editor.TextArea.Document.TextChanged += (_, _) =>
        {
            if (_updatingFromVm) return;
            if (DataContext is NoteEditorViewModel vm)
                vm.NoteContent = _editor.Text;
            UpdateLineNumbers();
        };

        // Sync ViewModel → editor (initial load + undo/redo)
        if (DataContext is NoteEditorViewModel { NoteContent: var initial })
        {
            _updatingFromVm = true;
            _editor.Text    = initial ?? string.Empty;
            _updatingFromVm = false;
        }

        // Watch VM for external content changes (undo/redo)
        DataContextChanged += (_, _) =>
        {
            if (DataContext is NoteEditorViewModel vm2)
            {
                vm2.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(NoteEditorViewModel.NoteContent) && !_updatingFromVm)
                    {
                        _updatingFromVm = true;
                        _editor.Text    = vm2.NoteContent ?? string.Empty;
                        _updatingFromVm = false;
                    }
                };
            }
        };

        // Keyboard shortcuts
        AddHandler(KeyDownEvent, OnKeyDown, handledEventsToo: true);

        // Wire toolbar buttons
        FindToolbarButtons();
        WireToolbarButtons();

        UpdateLineNumbers();
    }

    // ── Keyboard shortcuts ──────────────────────────────────

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not NoteEditorViewModel vm) return;

        if (e.KeyModifiers == KeyModifiers.Control)
        {
            switch (e.Key)
            {
                case Key.Z: vm.UndoCommand.Execute(null);              e.Handled = true; break;
                case Key.Y: vm.RedoCommand.Execute(null);              e.Handled = true; break;
                case Key.S: vm.SaveCommand.Execute(null);              e.Handled = true; break;
                case Key.B: WrapSelection("**", "**");                 e.Handled = true; break;
                case Key.I: WrapSelection("*", "*");                   e.Handled = true; break;
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

    // ── Toolbar wiring ──────────────────────────────────────

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
        _italicBtn?.Click    += (_, _) => WrapSelection("*", "*");
        _strikeBtn?.Click    += (_, _) => WrapSelection("~~", "~~");
        _codeBtn?.Click      += (_, _) => WrapSelection("`", "`");
        _h1Btn?.Click        += (_, _) => PrependLine("# ");
        _h2Btn?.Click        += (_, _) => PrependLine("## ");
        _h3Btn?.Click        += (_, _) => PrependLine("### ");
        _bulletBtn?.Click    += (_, _) => PrependLine("- ");
        _numberedBtn?.Click  += (_, _) => PrependLine("1. ");
        _checklistBtn?.Click += (_, _) => PrependLine("- [ ] ");
        _quoteBtn?.Click     += (_, _) => PrependLine("> ");
        _tableBtn?.Click     += (_, _) => InsertAtCursor("\n| Header 1 | Header 2 |\n| --- | --- |\n| Cell | Cell |\n");
        _hrBtn?.Click        += (_, _) => InsertAtCursor("\n---\n");
    }

    // ── Editor helpers ──────────────────────────────────────

    /// <summary>
    /// Wraps the selected text with open/close markers.
    /// If nothing is selected, inserts the markers at the cursor.
    /// </summary>
    private void WrapSelection(string open, string close)
    {
        if (_editor == null) return;
        var sel = _editor.TextArea.Selection;

        if (!sel.IsEmpty)
        {
            var start   = _editor.Document.GetOffset(sel.StartPosition.Location);
            var end     = _editor.Document.GetOffset(sel.EndPosition.Location);
            if (start > end) (start, end) = (end, start);
            var selected = _editor.Document.GetText(start, end - start);
            _editor.Document.Replace(start, end - start, open + selected + close);
            _editor.TextArea.Caret.Offset = start + open.Length + selected.Length;
        }
        else
        {
            var pos = _editor.TextArea.Caret.Offset;
            _editor.Document.Insert(pos, open + close);
            _editor.TextArea.Caret.Offset = pos + open.Length;
        }
        _editor.Focus();
    }

    /// <summary>
    /// Prepends a prefix to the line the cursor is currently on.
    /// </summary>
    private void PrependLine(string prefix)
    {
        if (_editor == null) return;
        var line = _editor.Document.GetLineByOffset(_editor.TextArea.Caret.Offset);
        _editor.Document.Insert(line.Offset, prefix);
        _editor.TextArea.Caret.Offset = line.Offset + prefix.Length;
        _editor.Focus();
    }

    /// <summary>
    /// Inserts text at the current cursor position.
    /// </summary>
    private void InsertAtCursor(string text)
    {
        if (_editor == null) return;
        var pos = _editor.TextArea.Caret.Offset;
        _editor.Document.Insert(pos, text);
        _editor.TextArea.Caret.Offset = pos + text.Length;
        _editor.Focus();
    }

    private void UpdateLineNumbers()
    {
        if (_lineNumbers == null || _editor == null) return;
        var count = _editor.Document.LineCount;
        _lineNumbers.Text = string.Join(Environment.NewLine, Enumerable.Range(1, count));
    }

    // ── References panel click handler ──────────────────────

    private void ReferencedNote_Tapped(object? sender, TappedEventArgs e)
    {
        if (sender is TextBlock tb && tb.DataContext is NoteCard card &&
            DataContext is NoteEditorViewModel vm)
        {
            vm.OpenReferencedNoteCommand.Execute(card);
        }
    }
}
