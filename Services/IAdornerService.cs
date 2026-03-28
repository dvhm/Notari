using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Notari.Services;

public interface IAdornerService
{
    void Initialize(RichTextBox editor);
    void SetHighlightBrush(SolidColorBrush brush);
    void SetHighlights(IReadOnlyList<Rect> rects);
    void SetGutterEntries(IReadOnlyList<(double Y, int Syllables)> entries);
    void SetDimRanges(IReadOnlyList<Rect> rects);
    void SetFindHighlights(IReadOnlyList<Rect> matches, Rect? active);
    void SetRhymeLabels(IReadOnlyList<(double Y, double X, string Label)> labels);
    void ClearOverlays();
}
