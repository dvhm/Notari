using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Notari;

public partial class MainWindow
{
    private List<(TextPointer Start, TextPointer End)> _matches = [];
    private int _matchIndex = -1;
    private bool _findBarOpen = false;
    private readonly DispatcherTimer _findDebounce = new() { Interval = TimeSpan.FromMilliseconds(180) };

    private void InitFindReplace()
    {
        _findDebounce.Tick += (_, _) => { _findDebounce.Stop(); RefreshMatches(); };
    }

    private void OnFind(object sender, ExecutedRoutedEventArgs e)    => OpenFindBar(showReplace: false);
    private void OnReplace(object sender, ExecutedRoutedEventArgs e) => OpenFindBar(showReplace: true);

    private void OpenFindBar(bool showReplace)
    {
        _findBarOpen = true;
        _adorner?.SetHighlights([]);
        ReplaceRow.Visibility = showReplace ? Visibility.Visible : Visibility.Collapsed;
        FindBar.Visibility = Visibility.Visible;
        FindInput.Focus();
        FindInput.SelectAll();
        RefreshMatches();
    }

    private void OnFindClose(object sender, RoutedEventArgs e) => CloseFindBar();

    private void CloseFindBar()
    {
        _findBarOpen = false;
        _findDebounce.Stop();
        FindBar.Visibility = Visibility.Collapsed;
        _matches.Clear();
        _matchIndex = -1;
        _adorner?.SetFindHighlights([], null);
        Editor.Focus();
    }

    private void OnFindTextChanged(object sender, TextChangedEventArgs e)
    {
        _findDebounce.Stop();
        _findDebounce.Start();
    }

    private void OnFindOptionChanged(object sender, RoutedEventArgs e) => RefreshMatches();

    private void OnFindInputKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Shift)
            { FindPrev(); e.Handled = true; }
        else if (e.Key == Key.Enter)
            { FindNext(); e.Handled = true; }
        else if (e.Key == Key.Escape)
            { CloseFindBar(); e.Handled = true; }
    }

    private void OnFindNext(object sender, RoutedEventArgs e) => FindNext();
    private void OnFindPrev(object sender, RoutedEventArgs e) => FindPrev();

    private void FindNext()
    {
        if (_matches.Count == 0) return;
        _matchIndex = (_matchIndex + 1) % _matches.Count;
        SelectMatch(_matchIndex);
    }

    private void FindPrev()
    {
        if (_matches.Count == 0) return;
        _matchIndex = (_matchIndex - 1 + _matches.Count) % _matches.Count;
        SelectMatch(_matchIndex);
    }

    private void SelectMatch(int index)
    {
        var (start, _) = _matches[index];
        start.Paragraph?.BringIntoView();
        UpdateFindHighlights();
        UpdateMatchLabel();
    }

    private static Rect? GetMatchRect(TextPointer start, TextPointer end)
    {
        var r0 = start.GetCharacterRect(LogicalDirection.Forward);
        var r1 = end.GetCharacterRect(LogicalDirection.Backward);
        if (r0.IsEmpty || r1.IsEmpty) return null;
        return new Rect(r0.Left, r0.Top, r1.Right - r0.Left, r0.Height);
    }

    private void UpdateFindHighlights()
    {
        if (_adorner is null) return;
        var allRects = new List<Rect>(_matches.Count);
        Rect? activeRect = null;
        for (int i = 0; i < _matches.Count; i++)
        {
            var r = GetMatchRect(_matches[i].Start, _matches[i].End);
            if (r is Rect rect)
            {
                allRects.Add(rect);
                if (i == _matchIndex) activeRect = rect;
            }
        }
        _adorner.SetFindHighlights(allRects, activeRect);
    }

    private void RefreshMatches()
    {
        _matches.Clear();
        _matchIndex = -1;

        string term = FindInput.Text;
        if (string.IsNullOrEmpty(term)) { _adorner?.SetFindHighlights([], null); UpdateMatchLabel(); return; }

        bool matchCase  = MatchCaseCheck.IsChecked  == true;
        bool wholeWords = WholeWordCheck.IsChecked   == true;
        var comparison  = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        var pointer = Editor.Document.ContentStart;
        while (pointer != null && pointer.CompareTo(Editor.Document.ContentEnd) < 0)
        {
            if (pointer.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
            {
                string run = pointer.GetTextInRun(LogicalDirection.Forward);
                int idx = 0;
                while (true)
                {
                    idx = run.IndexOf(term, idx, comparison);
                    if (idx < 0) break;

                    if (wholeWords)
                    {
                        bool leftOk  = idx == 0                        || !char.IsLetterOrDigit(run[idx - 1]);
                        bool rightOk = idx + term.Length >= run.Length  || !char.IsLetterOrDigit(run[idx + term.Length]);
                        if (!leftOk || !rightOk) { idx += term.Length; continue; }
                    }

                    var start = pointer.GetPositionAtOffset(idx);
                    var end   = pointer.GetPositionAtOffset(idx + term.Length);
                    if (start != null && end != null)
                        _matches.Add((start, end));

                    idx += term.Length;
                }
            }
            pointer = pointer.GetNextContextPosition(LogicalDirection.Forward);
        }

        if (_matches.Count > 0)
        {
            _matchIndex = 0;
            SelectMatch(0);
        }
        else
        {
            _adorner?.SetFindHighlights([], null);
            UpdateMatchLabel();
        }
    }

    private void UpdateMatchLabel()
    {
        if (_matches.Count == 0)
            MatchLabel.Text = string.IsNullOrEmpty(FindInput.Text) ? "" : "No results";
        else
            MatchLabel.Text = $"{_matchIndex + 1}/{_matches.Count}";
    }

    private void OnReplaceOne(object sender, RoutedEventArgs e)
    {
        if (_matches.Count == 0) return;
        int idx = _matchIndex >= 0 ? _matchIndex : 0;
        var (start, end) = _matches[idx];
        Editor.BeginChange();
        new TextRange(start, end).Text = ReplaceInput.Text;
        Editor.EndChange();
        RefreshMatches();
    }

    private void OnReplaceAll(object sender, RoutedEventArgs e)
    {
        if (_matches.Count == 0) return;
        Editor.BeginChange();
        for (int i = _matches.Count - 1; i >= 0; i--)
        {
            var (start, end) = _matches[i];
            new TextRange(start, end).Text = ReplaceInput.Text;
        }
        Editor.EndChange();
        RefreshMatches();
    }
}
