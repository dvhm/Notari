using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Notari.Services;

/// <summary>
/// Provides text analysis operations bound to a specific <see cref="RichTextBox"/>.
/// All methods must be called on the UI thread.
/// </summary>
public sealed class DocumentService : IDocumentService
{
    private readonly RichTextBox _editor;
    private readonly ITextAnalysisService _text;

    public DocumentService(RichTextBox editor, ITextAnalysisService text)
    {
        _editor = editor;
        _text   = text;
    }

    public string GetActiveWord()
    {
        var selection = _editor.Selection;
        if (!selection.IsEmpty)
        {
            string raw = selection.Text.Trim()
                .Split([' ', '\t', '\n', '\r', '-'], StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault() ?? string.Empty;
            return _text.CleanWord(raw);
        }
        return GetWordAtPointer(selection.Start);
    }

    /// <summary>
    /// Extracts the word surrounding <paramref name="pointer"/> by fetching the paragraph text
    /// once and walking it as a plain string — avoids per-character TextPointer allocations.
    /// </summary>
    public string GetWordAtPointer(TextPointer pointer)
    {
        var para = pointer.Paragraph;
        if (para is null) return string.Empty;

        string paraText   = new TextRange(para.ContentStart, para.ContentEnd).Text;
        string textBefore = new TextRange(para.ContentStart, pointer).Text;
        int    idx        = Math.Clamp(textBefore.Length, 0, paraText.Length);

        int start = idx;
        while (start > 0 && !char.IsWhiteSpace(paraText[start - 1]) && paraText[start - 1] != '-')
            start--;

        int end = idx;
        while (end < paraText.Length && !char.IsWhiteSpace(paraText[end]) && paraText[end] != '-')
            end++;

        return _text.CleanWord(paraText[start..end]);
    }

    public List<Rect> FindWordRects(string word)
    {
        var rects = new List<Rect>();
        int len   = word.Length;

        foreach (var run in GetAllRuns())
        {
            string text = run.Text;
            int idx = 0;
            while ((idx = text.IndexOf(word, idx, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                bool prevOk = idx == 0             || !char.IsLetterOrDigit(text[idx - 1]);
                bool nextOk = idx + len >= text.Length || !char.IsLetterOrDigit(text[idx + len]);

                if (prevOk && nextOk)
                {
                    var startPtr = run.ContentStart.GetPositionAtOffset(idx);
                    var endPtr   = run.ContentStart.GetPositionAtOffset(idx + len);
                    if (startPtr is not null && endPtr is not null)
                    {
                        var r0 = startPtr.GetCharacterRect(LogicalDirection.Forward);
                        var r1 = endPtr.GetCharacterRect(LogicalDirection.Backward);
                        if (!r0.IsEmpty && !r1.IsEmpty)
                            rects.Add(new Rect(r0.Left, r0.Top, r1.Right - r0.Left, r0.Height));
                    }
                }
                idx++;
            }
        }

        return rects;
    }

    public List<Rect> FindBracketRects(Regex pattern)
    {
        var rects = new List<Rect>();

        foreach (var run in GetAllRuns())
        {
            string text = run.Text;
            foreach (System.Text.RegularExpressions.Match m in pattern.Matches(text))
            {
                var startPtr = run.ContentStart.GetPositionAtOffset(m.Index);
                var endPtr   = run.ContentStart.GetPositionAtOffset(m.Index + m.Length);
                if (startPtr is not null && endPtr is not null)
                {
                    var r0 = startPtr.GetCharacterRect(LogicalDirection.Forward);
                    var r1 = endPtr.GetCharacterRect(LogicalDirection.Backward);
                    if (!r0.IsEmpty && !r1.IsEmpty)
                        rects.Add(new Rect(r0.Left, r0.Top, r1.Right - r0.Left, r0.Height));
                }
            }
        }

        return rects;
    }

    public (int WordCount, int LineCount) GetDocumentStats()
    {
        var text  = new TextRange(_editor.Document.ContentStart, _editor.Document.ContentEnd).Text;
        int words = text.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries).Length;
        int lines = _editor.Document.Blocks.OfType<Paragraph>()
            .Sum(p => 1 + _text.FlattenInlines(p.Inlines).OfType<LineBreak>().Count());
        return (words, lines);
    }

    public List<(double Y, string Text)> GetSyllableSegments() =>
        _editor.Document.Blocks
            .OfType<Paragraph>()
            .SelectMany(_text.GetLineSegments)
            .Select(s =>
            {
                var rect = s.Start.GetCharacterRect(LogicalDirection.Forward);
                return (Y: rect.Top, s.Text, Valid: !rect.IsEmpty);
            })
            .Where(s => s.Valid)
            .Select(s => (s.Y, s.Text))
            .ToList();

    public List<(double Y, double X, string LastWord)> GetRhymeSchemeSegments() =>
        _editor.Document.Blocks
            .OfType<Paragraph>()
            .SelectMany(_text.GetLineSegments)
            .Select(s =>
            {
                var startRect = s.Start.GetCharacterRect(LogicalDirection.Forward);
                var endRect   = s.End.GetCharacterRect(LogicalDirection.Backward);
                return (Y: startRect.Top, X: endRect.Right,
                    LastWord: _text.GetLastWordOf(s.Text), Valid: !startRect.IsEmpty);
            })
            .Where(s => s.Valid)
            .Select(s => (s.Y, s.X, s.LastWord))
            .ToList();

    public void ScrollToTypewriterPosition(ScrollViewer canvas)
    {
        var caretRect = _editor.CaretPosition?.GetCharacterRect(LogicalDirection.Forward) ?? Rect.Empty;
        if (caretRect.IsEmpty) return;
        var transform   = _editor.TransformToAncestor(canvas);
        var caretCenter = transform.Transform(new Point(0, caretRect.Top + caretRect.Height / 2));
        canvas.ScrollToVerticalOffset(caretCenter.Y - canvas.ViewportHeight / 2);
    }

    private IEnumerable<Run> GetAllRuns() =>
        _editor.Document.Blocks
            .OfType<Paragraph>()
            .SelectMany(p => _text.FlattenInlines(p.Inlines))
            .OfType<Run>();
}
