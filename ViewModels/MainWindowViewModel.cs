using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using Notari.Models;
using Notari.Services;
using PhonKit;

namespace Notari.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly ILookupService      _lookup;
    private readonly IDocumentService    _doc;
    private readonly ITextAnalysisService _text;
    private readonly IAdornerService     _adorner;

    private CancellationTokenSource _lookupCts      = new();
    private CancellationTokenSource _syllableCts    = new();
    private CancellationTokenSource _hoverCts       = new();
    private CancellationTokenSource _rhymeSchemeCts = new();

    // ── Observable state ────────────────────────────────────────────────────

    private string _activeWord    = string.Empty;
    private bool   _isLookupBusy;
    private int    _wordCount;
    private int    _lineCount;
    private string _hoverWord = string.Empty;

    public string ActiveWord
    {
        get => _activeWord;
        private set => SetField(ref _activeWord, value);
    }

    public bool IsLookupBusy
    {
        get => _isLookupBusy;
        private set => SetField(ref _isLookupBusy, value);
    }

    public int WordCount
    {
        get => _wordCount;
        private set => SetField(ref _wordCount, value);
    }

    public int LineCount
    {
        get => _lineCount;
        private set => SetField(ref _lineCount, value);
    }

    public string HoverWord => _hoverWord;

    // ── Events for MainWindow UI side-effects ────────────────────────────────

    public event Action?                                                 LookupCleared;
    public event Action<LookupResult>?                                   LookupCompleted;
    public event Action<IReadOnlyList<(double Y, int Syl)>>?             SyllableCountsReady;
    public event Action<IReadOnlyList<(double Y, double X, string Label)>>? RhymeSchemeReady;
    public event Action<string, PhoneticInfo?>?                          HoverReady;
    public event Action?                                                 HoverCleared;

    public MainWindowViewModel(
        ILookupService       lookup,
        IDocumentService     doc,
        ITextAnalysisService text,
        IAdornerService      adorner)
    {
        _lookup  = lookup;
        _doc     = doc;
        _text    = text;
        _adorner = adorner;
    }

    // ── Async orchestration ──────────────────────────────────────────────────

    /// <summary>
    /// Debounces, resolves the active word, runs the full phonetic+semantic lookup,
    /// and fires <see cref="LookupCompleted"/> (or <see cref="LookupCleared"/> when empty).
    /// Must be called from the UI thread so DocumentService can access the RichTextBox after the await.
    /// </summary>
    public async Task OnSelectionChangedAsync(AppSettings settings, bool highlightEnabled, bool findBarOpen)
    {
        var ct = ReplaceCts(ref _lookupCts);

        try
        {
            await Task.Delay(settings.LookupDebounceMs, ct);

            string word = _doc.GetActiveWord();
            ActiveWord = word;

            if (string.IsNullOrWhiteSpace(word))
            {
                LookupCleared?.Invoke();
                return;
            }

            _adorner.SetHighlights(!findBarOpen && highlightEnabled ? _doc.FindWordRects(word) : []);

            IsLookupBusy = true;
            int limit    = settings.ResultLimit == 0 ? int.MaxValue : settings.ResultLimit;

            var minWait    = Task.Delay(500, ct);
            var resultTask = _lookup.LookupWordAsync(word, settings.SortByZipf, limit, ct);
            await Task.WhenAll(minWait, resultTask);
            ct.ThrowIfCancellationRequested();

            IsLookupBusy = false;
            LookupCompleted?.Invoke(resultTask.Result);

            _ = _lookup.EnrichWithSyllablesAsync(resultTask.Result.AllWordItems.ToList(), ct);
        }
        catch (OperationCanceledException)
        {
            IsLookupBusy = false;
        }
    }

    /// <summary>
    /// Debounces, collects line segments on the UI thread, counts syllables on a background thread,
    /// and fires <see cref="SyllableCountsReady"/>.
    /// </summary>
    public async Task UpdateSyllableCountsAsync(AppSettings settings)
    {
        var ct = ReplaceCts(ref _syllableCts);

        try
        {
            await Task.Delay(500, ct);

            var segments = _doc.GetSyllableSegments();
            var entries  = await Task.Run(() =>
                segments
                    .Select(s =>
                    {
                        var words = _text.StripBracketedContent(s.Text)
                            .Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries)
                            .Select(w => _text.CleanWord(w))
                            .Where(w => w.Length > 0);
                        int syl = words.Sum(w => _lookup.GetSyllableCount(w) ?? 0);
                        return (s.Y, Syl: syl);
                    })
                    .Where(e => e.Syl > 0)
                    .ToList()
            , ct);

            ct.ThrowIfCancellationRequested();
            SyllableCountsReady?.Invoke(entries);
        }
        catch (OperationCanceledException) { }
    }

    /// <summary>
    /// Debounces, collects rhyme-scheme segments on the UI thread, assigns letters on a background
    /// thread using <see cref="ILookupService.GetRhymeScore"/>, and fires <see cref="RhymeSchemeReady"/>.
    /// </summary>
    public async Task UpdateRhymeSchemeAsync()
    {
        var ct = ReplaceCts(ref _rhymeSchemeCts);

        try
        {
            await Task.Delay(500, ct);

            var segments = _doc.GetRhymeSchemeSegments();
            var labeled  = await Task.Run(() =>
            {
                var result = new List<(double Y, double X, string Label)>();
                var stanza = new List<(double Y, double X, string Word)>();

                foreach (var s in segments)
                {
                    if (s.LastWord.Length == 0)
                    {
                        result.AddRange(AssignRhymeLetters(stanza));
                        stanza.Clear();
                    }
                    else
                    {
                        stanza.Add((s.Y, s.X, s.LastWord));
                    }
                }
                result.AddRange(AssignRhymeLetters(stanza));
                return (IReadOnlyList<(double, double, string)>)result;
            }, ct);

            ct.ThrowIfCancellationRequested();
            RhymeSchemeReady?.Invoke(labeled);
        }
        catch (OperationCanceledException) { }
    }

    /// <summary>Fetches phonetics for <paramref name="word"/> and fires <see cref="HoverReady"/>.</summary>
    public async Task OnHoverWordAsync(string word)
    {
        var ct = ReplaceCts(ref _hoverCts);

        _hoverWord = word;

        try
        {
            var phonetics = await _lookup.GetPhoneticsAsync(word, ct);
            ct.ThrowIfCancellationRequested();
            HoverReady?.Invoke(word, phonetics.FirstOrDefault());
        }
        catch (OperationCanceledException) { }
    }

    public void UpdateDocumentStats()
    {
        var (words, lines) = _doc.GetDocumentStats();
        WordCount = words;
        LineCount = lines;
    }

    public void ClearHover()
    {
        _hoverWord = string.Empty;
        _hoverCts.Cancel();
        HoverCleared?.Invoke();
    }

    public void CancelAll()
    {
        _lookupCts.Cancel();
        _syllableCts.Cancel();
        _rhymeSchemeCts.Cancel();
        _hoverCts.Cancel();
    }

    public async ValueTask DisposeAsync()
    {
        CancelAll();
        _lookupCts.Dispose();
        _syllableCts.Dispose();
        _rhymeSchemeCts.Dispose();
        _hoverCts.Dispose();
        await _lookup.DisposeAsync();
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Assigns rhyme-scheme letters (A, B, C…) to each line within a stanza.
    /// Compares end-words using <see cref="ILookupService.GetRhymeScore"/>; any score above None counts as a match.
    /// Lines whose end-word is unknown to the database are omitted.
    /// </summary>
    private IReadOnlyList<(double Y, double X, string Label)> AssignRhymeLetters(
        List<(double Y, double X, string Word)> lines)
    {
        var groups     = new List<(char Letter, string Representative)>();
        char nextLetter = 'A';
        var result     = new List<(double Y, double X, string Label)>();

        foreach (var (y, x, word) in lines)
        {
            int matchIdx = -1;
            for (int i = 0; i < groups.Count; i++)
            {
                if (_lookup.GetRhymeScore(word, groups[i].Representative) != RhymeScore.None)
                {
                    matchIdx = i;
                    break;
                }
            }

            if (matchIdx >= 0)
            {
                result.Add((y, x, $"{groups[matchIdx].Letter}"));
            }
            else if (nextLetter <= 'Z')
            {
                char letter = nextLetter++;
                groups.Add((letter, word));
                result.Add((y, x, $"{letter}"));
            }
        }

        return result;
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    /// <summary>Cancels and disposes the current CTS, replaces it with a fresh one, and returns its token.</summary>
    private static CancellationToken ReplaceCts(ref CancellationTokenSource cts)
    {
        cts.Cancel();
        cts.Dispose();
        cts = new CancellationTokenSource();
        return cts.Token;
    }

    // ── INotifyPropertyChanged ────────────────────────────────────────────────

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
