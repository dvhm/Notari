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
        // Right edge of the left-gutter label — read from Geometry.xaml at construction time.
        private readonly double _gutterRightX;

        private readonly Typeface _typeface;
        private readonly double   _fontSize;
        private readonly Brush    _brush;
        private Brush _highlightBrush;
        private readonly Brush    _dimBrush;
        private readonly Brush    _findMatchBrush;
        private readonly Brush    _findActiveMatchBrush;
        // Colour palette for rhyme badges — one hue per letter, cycling after 10.
        private static readonly Brush[] _rhymeFgBrushes;
        private static readonly Brush[] _rhymeBgBrushes;
        private static readonly int     _rhymeColorCount;

        private IReadOnlyList<(double Y, int Syllables)>  _gutterEntries  = [];
        private IReadOnlyList<Rect>                       _highlights     = [];
        private IReadOnlyList<Rect>                       _dimRects       = [];
        private IReadOnlyList<Rect>                       _findMatches    = [];
        private Rect?                                     _findActive     = null;
        private IReadOnlyList<(double Y, double X, string Label)> _rhymeLabels = [];

        // Pre-built FormattedText caches — rebuilt when data changes, reused on every OnRender.
        private IReadOnlyList<(double Y, FormattedText Text)>            _gutterFormatted = [];
        private IReadOnlyList<(double Y, double X, FormattedText Text, Brush Fg, Brush Bg)> _rhymeFormatted  = [];

        static EditorAdorner()
        {
            var res = Application.Current.Resources;
            // Collect all Color.Rhyme.* keys in declaration order (A, B, C…)
            var colors = new List<Color>();
            for (char letter = 'A'; letter <= 'Z'; letter++)
            {
                string key = $"Color.Rhyme.{letter}";
                if (res.Contains(key))
                    colors.Add((Color)res[key]);
                else
                    break;
            }

            _rhymeColorCount = colors.Count;
            _rhymeFgBrushes  = new Brush[_rhymeColorCount];
            _rhymeBgBrushes  = new Brush[_rhymeColorCount];
            for (int i = 0; i < _rhymeColorCount; i++)
            {
                var c  = colors[i];
                var fg = new SolidColorBrush(c); fg.Freeze();
                var bg = new SolidColorBrush(Color.FromArgb(0x40, c.R, c.G, c.B)); bg.Freeze();
                _rhymeFgBrushes[i] = fg;
                _rhymeBgBrushes[i] = bg;
            }
        }

        public EditorAdorner(RichTextBox editor) : base(editor)
        {
            IsHitTestVisible = false;

            var res = Application.Current.Resources;
            _gutterRightX         = (double)res["Gutter.RightX"];
            _typeface             = new Typeface((FontFamily)res["Font.Primary"], FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            _fontSize             = (double)res["FontSize.XSmall"];
            _brush                = (Brush)res["Brush.TextSecondary"];
            _highlightBrush       = (Brush)res["Brush.Highlight"];
            _dimBrush             = (Brush)res["Brush.Dim"];
            _findMatchBrush       = (Brush)res["Brush.FindMatch"];
            _findActiveMatchBrush = (Brush)res["Brush.FindMatchActive"];
        }

        /// <summary>Sets per-paragraph syllable counts and schedules a redraw.</summary>
        public void SetGutterEntries(IReadOnlyList<(double Y, int Syllables)> entries)
        {
            _gutterEntries = entries;
            double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            _gutterFormatted = entries
                .Select(e => (e.Y, new FormattedText(
                    e.Syllables.ToString(),
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    _typeface, _fontSize, _brush, dpi)))
                .ToList();
            InvalidateVisual();
        }

        /// <summary>Sets word highlight rectangles. Pass an empty list to clear.</summary>
        public void SetHighlights(IReadOnlyList<Rect> rects)
        {
            _highlights = rects;
            InvalidateVisual();
        }

        public void SetHighlightBrush(Brush brush)
        {
            _highlightBrush = brush;
            InvalidateVisual();
        }

        /// <summary>Sets dim overlay rectangles for bracketed content. Pass an empty list to clear.</summary>
        public void SetDimRanges(IReadOnlyList<Rect> rects)
        {
            _dimRects = rects;
            InvalidateVisual();
        }

        /// <summary>Sets inline rhyme-scheme labels (e.g. "A", "B"). Pass an empty list to clear.</summary>
        public void SetRhymeLabels(IReadOnlyList<(double Y, double X, string Label)> labels)
        {
            _rhymeLabels = labels;
            double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            _rhymeFormatted = labels
                .Select(e =>
                {
                    int   idx = (e.Label.Length > 0 ? e.Label[0] - 'A' : 0) % _rhymeColorCount;
                    Brush fg  = _rhymeFgBrushes[idx];
                    Brush bg  = _rhymeBgBrushes[idx];
                    var   ft  = new FormattedText(
                        e.Label,
                        CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        _typeface, _fontSize, fg, dpi);
                    return (e.Y, e.X, ft, fg, bg);
                })
                .ToList();
            InvalidateVisual();
        }

        /// <summary>Sets find-match highlight rectangles. Pass empty list and null to clear.</summary>
        public void SetFindHighlights(IReadOnlyList<Rect> matches, Rect? active)
        {
            _findMatches = matches;
            _findActive  = active;
            InvalidateVisual();
        }

        /// <summary>Clears all overlays in one pass with a single redraw.</summary>
        public void ClearOverlays()
        {
            _highlights     = [];
            _dimRects       = [];
            _rhymeLabels    = [];
            _rhymeFormatted = [];
            _gutterEntries  = [];
            _gutterFormatted = [];
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            foreach (var rect in _dimRects)
                dc.DrawRectangle(_dimBrush, null, rect);

            foreach (var rect in _highlights)
                dc.DrawRoundedRectangle(_highlightBrush, null, rect, 2, 2);

            foreach (var rect in _findMatches)
                dc.DrawRoundedRectangle(_findMatchBrush, null, rect, 2, 2);

            if (_findActive is Rect active)
                dc.DrawRoundedRectangle(_findActiveMatchBrush, null, active, 2, 2);

            foreach (var (y, text) in _gutterFormatted)
                dc.DrawText(text, new Point(_gutterRightX - text.Width, y));

            foreach (var (y, x, text, _, bg) in _rhymeFormatted)
            {
                const double padX = 5, padY = 1, offsetY = 4;
                double bx = x + 9;
                var badgeRect = new Rect(bx - padX, y - padY + offsetY, text.Width + padX * 2, text.Height + padY * 2);
                dc.DrawRoundedRectangle(bg, null, badgeRect, 3, 3);
                dc.DrawText(text, new Point(bx, y + offsetY));
            }
        }
    }
}
