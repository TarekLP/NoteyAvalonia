using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Transformation;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NoteToolAvalonia.Models;
using NoteToolAvalonia.ViewModels;

namespace NoteToolAvalonia.Views;

public partial class WelcomeView : UserControl
{
    private bool _introPlayed;

    public WelcomeView()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, OnKeyDown, handledEventsToo: true);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (_introPlayed) return;
        _introPlayed = true;
        Dispatcher.UIThread.Post(PlayCardIntro, DispatcherPriority.Background);
    }

    private async void PlayCardIntro()
    {
        var cards = new List<(Border card, int delay)>();
        int delay = 0;
        foreach (var container in NotesList.GetRealizedContainers())
        {
            if (FindCardRoot(container) is not { } card) continue;

            card.Opacity = 0;
            card.RenderTransform = Translate(0, 24);
            cards.Add((card, delay++));
        }

        foreach (var (card, d) in cards)
        {
            await Task.Delay(90 * d);
            AnimateCardIn(card);
        }
    }

    private static Border? FindCardRoot(Control container)
        => container.GetVisualDescendants().OfType<Border>().FirstOrDefault();

    private static TransformOperations Translate(double x, double y)
    {
        var builder = new TransformOperations.Builder(1);
        builder.AppendTranslate(x, y);
        return builder.Build();
    }

    private static void AnimateCardIn(Border card)
    {
        card.Opacity = 1;
        card.RenderTransform = Translate(0, 0);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (DataContext is not WelcomeViewModel vm) return;
        if (!vm.IsCreatingNote) return;

        if (vm.CreatorStep < 4)
            vm.CreatorNextCommand.Execute(null);
        else
            vm.CreateNoteCommand.Execute(null);

        e.Handled = true;

        // Focus the TextBox for whichever step we just moved to.
        // CreatorStep is already incremented by CreatorNextCommand above.
        var targetStep = vm.CreatorStep;
        Dispatcher.UIThread.Post(() =>
        {
            var box = this.FindControl<TextBox>($"WizardStep{targetStep}Box");
            box?.Focus();
        }, DispatcherPriority.Input);
    }

    private void NoteCard_Tapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border b && b.DataContext is NoteCard card &&
            DataContext is WelcomeViewModel vm)
        {
            vm.OpenNoteCommand.Execute(card);
        }
    }
}