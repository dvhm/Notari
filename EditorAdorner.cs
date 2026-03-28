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
        private Brush _highlightBrush;
        private readonly Brush    _dimBrush;
        private readonly Brush    _findMatchBrush;
        private readonly Brush    _findActiveMatchBrush;
        // Colour palette for rhyme badges — one hue per letter, cycling after 10.
        private static readonly Color[] RhymeColors =
        [
            Color.FromRgb(0x4F, 0xC3, 0xF7), // A – sky blue
            Color.FromRgb(0x81, 0xC7, 0x84), // B – green
            Color.FromRgb(0xFF, 0xB7, 0x4D), // C – amber
            Color.FromRgb(0xCE, 0x93, 0xD8), // D – purple
            Color.FromRgb(0xEF, 0x83, 0x89), // E – rose
            Color.FromRgb(0x4D, 0xD0, 0xE1), // F – cyan
            Color.FromRgb(0xF0, 0x62, 0x92), // G – pink
            Color.FromRgb(0xDC, 0xE7, 0x75), // H – lime
            Color.FromRgb(0x79, 0x86, 0xCB), // I – indigo
            Color.FromRgb(0xFF, 0x8A, 0x65), // J – deep orange
        ];

        private static readonly Brush[] _rhymeFgBrushes;
        private static readonly Brush[] _rhymeBgBrushes;

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
            _rhymeFgBrushes = new Brush[RhymeColors.Length];
            _rhymeBgBrushes = new Brush[RhymeColors.Length];
            for (int i = 0; i < RhymeColors.Length; i++)
            {
                var c  = RhymeColors[i];
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
                    int   idx = (e.Label.Length > 0 ? e.Label[0] - 'A' : 0) % RhymeColors.Length;
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
                dc.DrawText(text, new Point(GutterRightX - text.Width, y));

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
