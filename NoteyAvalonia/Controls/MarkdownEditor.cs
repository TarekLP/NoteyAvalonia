using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Markdig;
using Markdig.Extensions.TaskLists;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using MdBlock    = Markdig.Syntax.Block;
using MdInline   = Markdig.Syntax.Inlines.Inline;
using MdLeaf     = Markdig.Syntax.LeafBlock;
using MdContainerInline = Markdig.Syntax.Inlines.ContainerInline;

namespace NoteToolAvalonia.Controls;

// ponytail: True-WYSIWYG Markdown editor.
//
//  - Source of truth: a TextBox bound to the `Markdown` DP. The TextBox is
//    always visible. There's no "display text with markers stripped" — the
//    caret and selection live on the raw source, so wrap/prepend/insert never
//    have to map display→raw.
//
//  - Live preview: a SelectableTextBlock whose Inlines are rebuilt from the
//    markdig AST. The rebuild is debounced (~120ms) so typing stays smooth.
//
//  - Render styles: a small built-in style map (heading sizes, code font,
//    blockquote bar, task-list checkboxes). Extend the map to add more.
//
//  - Pipeline: UseAdvancedExtensions so tables, task lists, autolinks, etc.
//    Just works. Change the pipeline in ctor to add/remove extensions.
public class MarkdownEditor : TemplatedControl
{
    public static readonly StyledProperty<string?> MarkdownProperty =
        AvaloniaProperty.Register<MarkdownEditor, string?>(nameof(Markdown));

    public static readonly StyledProperty<bool> ShowPreviewProperty =
        AvaloniaProperty.Register<MarkdownEditor, bool>(nameof(ShowPreview), true);

    public string? Markdown
    {
        get => GetValue(MarkdownProperty);
        set => SetValue(MarkdownProperty, value);
    }

    public bool ShowPreview
    {
        get => GetValue(ShowPreviewProperty);
        set => SetValue(ShowPreviewProperty, value);
    }

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    private TextBox? _source;
    private SelectableTextBlock? _preview;
    private Grid?   _root;
    private GridSplitter? _splitter;
    private DispatcherTimer? _debounce;

    private bool _suppressTextChanged;

    public MarkdownEditor()
    {
        // The visual tree is built by the template (MarkdownEditorOnly.axaml).
        // For designer / non-templated use, fall back to a programmatic layout.
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _root     = e.NameScope.Find<Grid>("PART_Root");
        _source   = e.NameScope.Find<TextBox>("PART_Source");
        _preview  = e.NameScope.Find<SelectableTextBlock>("PART_Preview");
        _splitter = e.NameScope.Find<GridSplitter>("PART_Splitter");

        if (_source is not null)
        {
            _source.TextChanged += OnSourceTextChanged;
            _source.Text = Markdown ?? string.Empty;
        }

        ApplyShowPreview();
        RenderPreview();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == MarkdownProperty)
        {
            var incoming = change.NewValue as string ?? string.Empty;
            if (_source is not null && _source.Text != incoming)
            {
                _suppressTextChanged = true;
                _source.Text = incoming;
                _suppressTextChanged = false;
            }
            QueueRender();
        }
        else if (change.Property == ShowPreviewProperty)
        {
            ApplyShowPreview();
        }
    }

    private void OnSourceTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_suppressTextChanged) return;
        var text = _source?.Text ?? string.Empty;
        SetCurrentValue(MarkdownProperty, text);
        QueueRender();
    }

    private void QueueRender()
    {
        if (_debounce is null)
        {
            _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
            _debounce.Tick += (_, _) => { _debounce!.Stop(); RenderPreview(); };
        }
        _debounce.Stop();
        _debounce.Start();
    }

    private void ApplyShowPreview()
    {
        if (_preview is null || _splitter is null) return;
        _preview.IsVisible = ShowPreview;
        _splitter.IsVisible = ShowPreview;
    }

    private void RenderPreview()
    {
        if (_preview is null) return;
        var text = Markdown ?? string.Empty;
        var doc  = Markdig.Markdown.Parse(text, Pipeline);
        var inlines = new List<Inline>();
        foreach (var block in doc)
            AppendBlock(block, inlines);
        _preview.Inlines = inlines;
    }

    // ── AST → Inlines ─────────────────────────────────────────────────────

    private void AppendBlock(MdBlock block, IList<Inline> output)
    {
        switch (block)
        {
            case HeadingBlock h:
            {
                var run = MakeRun(BlockText(h), h.Level switch
                {
                    1 => 28.0, 2 => 22.0, 3 => 18.0, 4 => 16.0, _ => 14.0
                });
                run.FontWeight = FontWeight.Bold;
                output.Add(run);
                output.Add(MakeLineBreak());
                break;
            }
            case ParagraphBlock p:
            {
                var p_inlines = new List<Inline>();
                foreach (var il in p.Inline) AppendInline(il, p_inlines);
                foreach (var x in p_inlines) output.Add(x);
                output.Add(MakeLineBreak());
                output.Add(MakeLineBreak());
                break;
            }
            case QuoteBlock q:
            {
                var q_inlines = new List<Inline>();
                foreach (var child in q) AppendBlock(child, q_inlines);
                var border = new Border
                {
                    BorderBrush = new SolidColorBrush(Color.FromRgb(179, 0, 0)),
                    BorderThickness = new Thickness(3, 0, 0, 0),
                    Padding = new Thickness(8, 0, 0, 0),
                    Child = new SelectableTextBlock { Inlines = q_inlines }
                };
                output.Add(new InlineUIContainer(border));
                output.Add(MakeLineBreak());
                break;
            }
            case CodeBlock cb:
            {
                var run = MakeRun(BlockText(cb), 13);
                run.FontFamily = new FontFamily("Cascadia Mono, Consolas, monospace");
                run.Background = new SolidColorBrush(Color.FromRgb(40, 40, 50));
                output.Add(run);
                output.Add(MakeLineBreak());
                output.Add(MakeLineBreak());
                break;
            }
            case ListBlock lb:
            {
                int n = 0;
                bool ordered = lb.IsOrdered;
                foreach (var item in lb)
                {
                    if (item is ListItemBlock li)
                    {
                        var prefix = ordered ? $"{++n}. " : "•  ";
                        var li_inlines = new List<Inline>();
                        foreach (var child in li) AppendBlock(child, li_inlines);
                        output.Add(MakeRun(prefix, 14));
                        foreach (var x in li_inlines) output.Add(x);
                    }
                }
                output.Add(MakeLineBreak());
                break;
            }
            case ThematicBreakBlock:
                output.Add(MakeRun("────────────────", 12));
                output.Add(MakeLineBreak());
                break;
            case TaskList tlb:
                foreach (var item in tlb.Children)
                {
                    if (item is TaskItem ti)
                    {
                        var box = ti.Checked ? "☑" : "☐";
                        output.Add(MakeRun(box + "  ", 14));
                        foreach (var il in ti.Inline ?? EnumerateEmpty()) AppendInline(il, output);
                    }
                }
                output.Add(MakeLineBreak());
                break;
            default:
                output.Add(MakeRun(BlockText(block), 14));
                output.Add(MakeLineBreak());
                break;
        }
    }

    private static IEnumerable<Inline> EnumerateEmpty() { yield break; }

    private void AppendInline(MdInline il, IList<Inline> output)
    {
        switch (il)
        {
            case LiteralInline lit:
                output.Add(MakeRun(lit.Content.ToString(), 14));
                break;
            case EmphasisInline em when em.DelimiterCount == 2:
            {
                var inner = new List<Inline>();
                foreach (var c in em) AppendInline(c, inner);
                foreach (var r in inner.OfType<Run>()) r.FontWeight = FontWeight.Bold;
                foreach (var x in inner) output.Add(x);
                break;
            }
            case EmphasisInline em:
            {
                var inner = new List<Inline>();
                foreach (var c in em) AppendInline(c, inner);
                foreach (var r in inner.OfType<Run>()) r.FontStyle = FontStyle.Italic;
                foreach (var x in inner) output.Add(x);
                break;
            }
            case CodeInline ci:
            {
                var r = MakeRun(ci.Content, 13);
                r.FontFamily = new FontFamily("Cascadia Mono, Consolas, monospace");
                r.Background = new SolidColorBrush(Color.FromRgb(40, 40, 50));
                output.Add(r);
                break;
            }
            case LinkInline li:
            {
                var linkText = new List<Inline>();
                if (li.IsImage) output.Add(MakeRun("🖼 ", 14));
                foreach (var c in li) AppendInline(c, linkText);
                if (!string.IsNullOrEmpty(li.Url))
                {
                    var url = MakeRun(li.Url, 13);
                    url.Foreground = new SolidColorBrush(Color.FromRgb(120, 170, 255));
                    linkText.Add(url);
                }
                foreach (var x in linkText) output.Add(x);
                break;
            }
            case LineBreakInline:
                output.Add(MakeLineBreak());
                break;
            default:
                if (il is ContainerInline ci2)
                    foreach (var c in ci2) AppendInline(c, output);
                break;
        }
    }

    private static Run MakeRun(string? text, double size)
    {
        var r = new Run { Text = text ?? string.Empty, FontSize = size };
        r.Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 230));
        return r;
    }

    private static InlineUIContainer MakeLineBreak() =>
        new(new TextBlock { Text = "\n", FontSize = 1 });

    private static string BlockText(MdBlock b)
    {
        // Walk inlines for blocks that don't expose LeafInline directly
        if (b is MdLeaf leaf && leaf.Inline is not null)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var il in leaf.Inline) sb.Append(InlineToString(il));
            return sb.ToString();
        }
        return string.Empty;
    }

    private static string InlineToString(MdInline il) => il switch
    {
        LiteralInline l => l.Content.ToString(),
        CodeInline c    => c.Content,
        LineBreakInline => "\n",
        MdContainerInline ci => Concat(ci),
        _ => string.Empty
    };

    private static string Concat(MdContainerInline ci)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var c in ci) sb.Append(InlineToString(c));
        return sb.ToString();
    }
}
