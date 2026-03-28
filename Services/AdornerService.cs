using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Notari.Services;

public sealed class AdornerService : IAdornerService
{
    private EditorAdorner? _adorner;

    public void Initialize(RichTextBox editor)
    {
        var layer = AdornerLayer.GetAdornerLayer(editor);
        if (layer is null) return;
        _adorner = new EditorAdorner(editor);
        layer.Add(_adorner);
    }

    public void SetHighlightBrush(SolidColorBrush brush) => _adorner?.SetHighlightBrush(brush);
    public void SetHighlights(IReadOnlyList<Rect> rects)  => _adorner?.SetHighlights(rects);
    public void SetGutterEntries(IReadOnlyList<(double Y, int Syllables)> entries) => _adorner?.SetGutterEntries(entries);
    public void SetDimRanges(IReadOnlyList<Rect> rects)   => _adorner?.SetDimRanges(rects);
    public void SetFindHighlights(IReadOnlyList<Rect> matches, Rect? active) => _adorner?.SetFindHighlights(matches, active);
    public void SetRhymeLabels(IReadOnlyList<(double Y, double X, string Label)> labels) => _adorner?.SetRhymeLabels(labels);
    public void ClearOverlays() => _adorner?.ClearOverlays();
}
