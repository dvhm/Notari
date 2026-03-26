using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Notari
{
    /// <summary>
    /// A transparent drawing layer on top of the editor RichTextBox.
    /// Coordinates are in the RichTextBox's local space (same system as TextPointer.GetCharacterRect).
    /// </summary>
    internal sealed class EditorAdorner : Adorner
    {
        // Right edge of the left-gutter label — sits 8px before text starts at x=80.
        private const double GutterRightX = 72.0;

        private readonly Typeface _typeface;
        private readonly double   _fontSize;
        private readonly Brush    _brush;
        private readonly Brush    _highlightBrush;

        private IReadOnlyList<(double Y, int Syllables)> _gutterEntries = [];
        private IReadOnlyList<Rect>                      _highlights    = [];

        public EditorAdorner(RichTextBox editor) : base(editor)
        {
            IsHitTestVisible = false;

            var res = Application.Current.Resources;
            _typeface       = new Typeface((FontFamily)res["Font.Primary"], FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            _fontSize       = (double)res["FontSize.XSmall"];
            _brush          = (Brush)res["Brush.TextSecondary"];
            _highlightBrush = (Brush)res["Brush.Highlight"];
        }

        /// <summary>Sets the per-paragraph syllable counts and schedules a redraw.</summary>
        public void SetGutterEntries(IReadOnlyList<(double Y, int Syllables)> entries)
        {
            _gutterEntries = entries;
            InvalidateVisual();
        }

        /// <summary>Sets word highlight rectangles. Pass an empty list to clear.</summary>
        public void SetHighlights(IReadOnlyList<Rect> rects)
        {
            _highlights = rects;
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;

            foreach (var rect in _highlights)
                dc.DrawRoundedRectangle(_highlightBrush, null, rect, 2, 2);

            foreach (var (y, syl) in _gutterEntries)
            {
                var text = new FormattedText(
                    syl.ToString(),
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    _typeface,
                    _fontSize,
                    _brush,
                    dpi);

                // Right-align the number within the gutter area.
                dc.DrawText(text, new Point(GutterRightX - text.Width, y));
            }
        }
    }
}
