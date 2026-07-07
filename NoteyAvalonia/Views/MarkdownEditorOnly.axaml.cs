using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using MarkdownUIComponent;

namespace NoteToolAvalonia.Views;

public partial class MarkdownEditorOnly : UserControl
{
    public MarkdownEditorOnly()
    {
        InitializeComponent();
        Loaded += (_, _) => FixEditorForeground();
    }

    // ponytail: the shipped MarkdownUIComponent.dll hardcodes
    // EditorInput.Foreground = 0x00FFFFFF (fully transparent ARGB) in
    // !XamlIlPopulate, so plain text is invisible until the WYSIWYG overlay
    // paints styled inlines. The overlay only repaints on Markdown-set /
    // TextChanged, so a fresh open shows nothing. Force an opaque brush so
    // user-typed characters are visible. Drop this when the control grows a
    // public theme / set_Foreground hook.
    private void FixEditorForeground()
    {
        var input = Inner.GetVisualDescendants()
                         .OfType<TextBox>()
                         .FirstOrDefault(t => t.Name == "EditorInput");
        if (input is not null)
            input.Foreground = Brushes.White;
    }
}
