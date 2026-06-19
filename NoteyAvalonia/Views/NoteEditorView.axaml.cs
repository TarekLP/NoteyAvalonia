using System;
using System.Linq;
using System.Globalization;
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

// ── bool → TextWrapping ─────────────────────────────────────
public class BoolToTextWrappingConverter : IValueConverter
{
    public object? Convert(object? v, Type t, object? p, CultureInfo c) =>
        v is true ? Avalonia.Media.TextWrapping.Wrap : Avalonia.Media.TextWrapping.NoWrap;
    public object? ConvertBack(object? v, Type t, object? p, CultureInfo c) =>
        throw new NotImplementedException();
}

// ════════════════════════════════════════════════════════════
//  MARKDOWN COLORIZING TRANSFORMER
// ════════════════════════════════════════════════════════════
public class MarkdownColorizingTransformer : DocumentColorizingTransformer
{
    private static readonly Color HeadingColor   = Color.Parse("#c792ea");
    private static readonly Color CodeColor      = Color.Parse("#89ddff");
    private static readonly Color LinkColor      = Color.Parse("#82aaff");
    private static readonly Color QuoteColor     = Color.Parse("#546e7a");
    private static readonly Color CheckDoneColor = Color.Parse("#2ed573");

    protected override void ColorizeLine(DocumentLine line)
    {
        var text = CurrentContext.Document.GetText(line.Offset, line.Length);
        if (text.Length == 0) return;

        if      (text.StartsWith("### ")) ApplyLine(line, HeadingColor, FontWeight.Bold);
        else if (text.StartsWith("## "))  ApplyLine(line, HeadingColor, FontWeight.Bold);
        else if (text.StartsWith("# "))   ApplyLine(line, HeadingColor, FontWeight.Bold);
        else if (text.StartsWith("> "))   ApplyLine(line, QuoteColor,   FontWeight.Normal);
        else if (text.TrimStart().StartsWith("- [x]", StringComparison.OrdinalIgnoreCase))
            ApplyLine(line, CheckDoneColor, FontWeight.Normal);

        ApplyBold(line, text);
        ApplyCode(line, text);
        ApplyLinks(line, text);
    }

    private void ApplyLine(DocumentLine line, Color color, FontWeight weight) =>
        ChangeLinePart(line.Offset, line.EndOffset, el =>
        {
            el.TextRunProperties.SetForegroundBrush(new SolidColorBrush(color));
            el.TextRunProperties.SetFontRenderingEmSize(((double)weight));
        });

    private void ApplyBold(DocumentLine line, string text)
    {
        int s = 0;
        while (true)
        {
            int o = text.IndexOf("**", s, StringComparison.Ordinal); if (o < 0) break;
            int c = text.IndexOf("**", o + 2, StringComparison.Ordinal); if (c < 0) break;
            ChangeLinePart(line.Offset + o, line.Offset + c + 2,
                el => el.TextRunProperties.SetFontRenderingEmSize(((double)FontWeight.Bold)));
            s = c + 2;
        }
    }

    private void ApplyCode(DocumentLine line, string text)
    {
        int s = 0;
        while (true)
        {
            int o = text.IndexOf('`', s); if (o < 0) break;
            int c = text.IndexOf('`', o + 1); if (c < 0) break;
            ChangeLinePart(line.Offset + o, line.Offset + c + 1,
                el => el.TextRunProperties.SetForegroundBrush(new SolidColorBrush(CodeColor)));
            s = c + 1;
        }
    }

    private void ApplyLinks(DocumentLine line, string text)
    {
        int s = 0;
        while (true)
        {
            int ob = text.IndexOf('[', s); if (ob < 0) break;
            int cb = text.IndexOf(']', ob); if (cb < 0) break;
            if (cb + 1 >= text.Length || text[cb + 1] != '(') { s = cb + 1; continue; }
            int cp = text.IndexOf(')', cb + 2); if (cp < 0) break;
            ChangeLinePart(line.Offset + ob, line.Offset + cp + 1, el =>
            {
                el.TextRunProperties.SetForegroundBrush(new SolidColorBrush(LinkColor));
                el.TextRunProperties.SetTextDecorations(TextDecorations.Underline);
            });
            s = cp + 1;
        }
    }
}

// ════════════════════════════════════════════════════════════
//  NOTE EDITOR VIEW
// ════════════════════════════════════════════════════════════
public partial class NoteEditorView : UserControl
{
    private TextEditor? _editor;
    private TextBlock?  _lineNumbers;
    private bool        _updatingFromVm;

    private Button? _boldBtn, _italicBtn, _strikeBtn, _codeBtn;
    private Button? _h1Btn, _h2Btn, _h3Btn;
    private Button? _bulletBtn, _numberedBtn, _checklistBtn, _quoteBtn;
    private Button? _tableBtn, _hrBtn;

    public NoteEditorView()
    {
        InitializeComponent();

        // DataContextChanged fires after the template applies the ViewModel,
        // guaranteeing DataContext is non-null when we set up the document.
        DataContextChanged += OnDataContextChanged;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _editor      = this.FindControl<TextEditor>("MarkdownEditor");
        _lineNumbers = this.FindControl<TextBlock>("LineNumbersPanel");

        if (_editor == null) return;

        // Fresh document prevents the input-blocked state that occurs when
        // AvaloniaEdit receives a null/shared document at construction time.
        _editor.Document = new TextDocument();

        _editor.TextArea.TextView.LineTransformers.Clear();
        _editor.TextArea.TextView.LineTransformers.Add(new MarkdownColorizingTransformer());

        AddHandler(KeyDownEvent, OnKeyDown, handledEventsToo: true);
        FindToolbarButtons();
        WireToolbarButtons();

        // DataContext may already be set — load content now if so.
        LoadContentFromVm();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is not NoteEditorViewModel vm) return;

        // Watch for undo/redo pushing new content in from the VM side
        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(NoteEditorViewModel.NoteContent) && !_updatingFromVm)
                LoadContentFromVm();
        };

        // Load content — may be a no-op if visual tree isn't ready yet;
        // OnAttachedToVisualTree will call it again when ready.
        LoadContentFromVm();
    }

    /// <summary>
    /// Pushes NoteContent from the ViewModel into the editor document.
    /// Assigns a fresh TextDocument each time to guarantee input works.
    /// Safe to call at any point — guards against null editor/DataContext.
    /// </summary>
    private void LoadContentFromVm()
    {
        if (_editor == null) return;
        if (DataContext is not NoteEditorViewModel vm) return;

        _updatingFromVm  = true;
        _editor.Document = new TextDocument(vm.NoteContent ?? string.Empty);

        // Re-attach TextChanged after replacing the document
        _editor.TextArea.Document.TextChanged += (_, _) =>
        {
            if (_updatingFromVm) return;
            if (DataContext is NoteEditorViewModel v)
                v.NoteContent = _editor.Text;
            UpdateLineNumbers();
        };

        _updatingFromVm = false;
        UpdateLineNumbers();

        // Focus the editor so the user can type immediately
        Avalonia.Threading.Dispatcher.UIThread.Post(
            () => _editor.Focus(),
            Avalonia.Threading.DispatcherPriority.Loaded);
    }

    // ── Keyboard shortcuts ──────────────────────────────────

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not NoteEditorViewModel vm) return;

        if (e.KeyModifiers == KeyModifiers.Control)
        {
            switch (e.Key)
            {
                case Key.Z: vm.UndoCommand.Execute(null);                 e.Handled = true; break;
                case Key.Y: vm.RedoCommand.Execute(null);                 e.Handled = true; break;
                case Key.S: vm.SaveCommand.Execute(null);                 e.Handled = true; break;
                case Key.B: WrapSelection("**", "**");                    e.Handled = true; break;
                case Key.I: WrapSelection("*",  "*");                     e.Handled = true; break;
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
    }

    // ── Text helpers ────────────────────────────────────────

    private void WrapSelection(string open, string close)
    {
        if (_editor == null) return;
        var sel = _editor.TextArea.Selection;
        if (!sel.IsEmpty)
        {
            var s    = _editor.Document.GetOffset(sel.StartPosition.Location);
            var end  = _editor.Document.GetOffset(sel.EndPosition.Location);
            if (s > end) (s, end) = (end, s);
            var selected = _editor.Document.GetText(s, end - s);
            _editor.Document.Replace(s, end - s, open + selected + close);
            _editor.TextArea.Caret.Offset = s + open.Length + selected.Length;
        }
        else
        {
            var pos = _editor.TextArea.Caret.Offset;
            _editor.Document.Insert(pos, open + close);
            _editor.TextArea.Caret.Offset = pos + open.Length;
        }
        _editor.Focus();
    }

    private void PrependLine(string prefix)
    {
        if (_editor == null) return;
        var line = _editor.Document.GetLineByOffset(_editor.TextArea.Caret.Offset);
        _editor.Document.Insert(line.Offset, prefix);
        _editor.TextArea.Caret.Offset = line.Offset + prefix.Length;
        _editor.Focus();
    }

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
        _lineNumbers.Text = string.Join(
            Environment.NewLine,
            Enumerable.Range(1, _editor.Document.LineCount));
    }

    // ── References panel ────────────────────────────────────

    private void ReferencedNote_Tapped(object? sender, TappedEventArgs e)
    {
        if (sender is TextBlock tb && tb.DataContext is NoteCard card &&
            DataContext is NoteEditorViewModel vm)
            vm.OpenReferencedNoteCommand.Execute(card);
    }
}
