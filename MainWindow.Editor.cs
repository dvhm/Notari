using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Notari.Models;

namespace Notari
{
    public partial class MainWindow : Window
    {
        private void OnEditorTextChanged(object sender, TextChangedEventArgs e)
        {
            _statsDebounce.Stop();
            _statsDebounce.Start();
            SetDirty(true);
            _adorner?.SetHighlights([]);
            _adorner?.SetDimRanges([]);
            _adorner?.SetRhymeLabels([]);
            _adorner?.SetGutterEntries([]);
            UpdateSyllableCounts();
            UpdateRhymeScheme();
            ScrollToTypewriterPosition();
        }

        private readonly System.Windows.Threading.DispatcherTimer _statsDebounce = new()
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };

        private void UpdateDocumentStats()
        {
            var text = new TextRange(Editor.Document.ContentStart, Editor.Document.ContentEnd).Text;
            _wordCount = text.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries).Length;
            _lineCount = Editor.Document.Blocks.OfType<Paragraph>()
                .Sum(p => 1 + FlattenInlines(p.Inlines).OfType<LineBreak>().Count());
            UpdateTitle();
        }

        private void InitAdorner()
        {
            var layer = AdornerLayer.GetAdornerLayer(Editor);
            if (layer is null) return;
            _adorner = new EditorAdorner(Editor);
            layer.Add(_adorner);
        }

        private async void UpdateSyllableCounts()
        {
            if (SyllableToggle?.IsChecked != true)
            {
                _adorner?.SetGutterEntries([]);
                return;
            }
            _syllableCts.Cancel();
            _syllableCts.Dispose();
            _syllableCts = new CancellationTokenSource();
            var ct = _syllableCts.Token;

            try
            {
                await Task.Delay(500, ct);

                // Collect line segments on the UI thread, splitting at LineBreak elements.
                var segments = Editor.Document.Blocks
                    .OfType<Paragraph>()
                    .SelectMany(GetLineSegments)
                    .Select(s =>
                    {
                        var rect = s.Start.GetCharacterRect(LogicalDirection.Forward);
                        return (Y: rect.Top, s.Text, Valid: !rect.IsEmpty);
                    })
                    .Where(s => s.Valid)
                    .ToList();

                // Compute syllable counts on a background thread.
                var entries = await Task.Run(() =>
                    segments
                        .Select(s =>
                        {
                            var words = _bracketedContent.Replace(s.Text, " ")
                                .Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries)
                                .Select(w => new string(w.Where(c => char.IsLetterOrDigit(c) || c == '\'').ToArray()))
                                .Where(w => w.Length > 0);
                            int syl = words.Sum(w => _db.GetSyllableCount(w) ?? 0);
                            return (s.Y, Syl: syl);
                        })
                        .Where(e => e.Syl > 0)
                        .ToList()
                , ct);

                ct.ThrowIfCancellationRequested();
                _adorner?.SetGutterEntries(entries);
                if (_settings.DimBrackets)
                    _adorner?.SetDimRanges(FindBracketRects());
                else
                    _adorner?.SetDimRanges([]);
            }
            catch (OperationCanceledException) { }
        }

        private static readonly Regex _bracketedContent = new(@"\[[^\]]*\]|\([^\)]*\)", RegexOptions.Compiled);
        private Regex _allBrackets = new(@"(?!x)x", RegexOptions.Compiled);

        private static readonly Dictionary<(bool, bool, bool), Regex> _bracketRegexCache = [];

        private static Regex BuildBracketRegex(AppSettings s)
        {
            var key = (s.DimSquare, s.DimRound, s.DimCurly);
            if (_bracketRegexCache.TryGetValue(key, out var cached))
                return cached;

            var parts = new List<string>();
            if (s.DimSquare) parts.Add(@"\[[^\]]*\]");
            if (s.DimRound)  parts.Add(@"\([^\)]*\)");
            if (s.DimCurly)  parts.Add(@"\{[^\}]*\}");
            var regex = parts.Count > 0
                ? new Regex(string.Join("|", parts), RegexOptions.Compiled)
                : new Regex(@"(?!x)x", RegexOptions.Compiled);
            _bracketRegexCache[key] = regex;
            return regex;
        }

        internal void ApplySettings(AppSettings s, bool save = true)
        {
            _settings = s;
            if (save) s.Save();

            _allBrackets = BuildBracketRegex(s);

            // Accent color — replace brush entries so DynamicResource bindings update everywhere,
            // and derive Brush.Highlight as the accent at 40% alpha (used by adorner + toggle button)
            if (System.Windows.Media.ColorConverter.ConvertFromString(s.AccentColor) is System.Windows.Media.Color accent)
            {
                Application.Current.Resources["Brush.Primary"] = new System.Windows.Media.SolidColorBrush(accent);
                static byte Lighten(byte ch) => (byte)Math.Min(255, ch + (255 - ch) / 4);
                var hover = System.Windows.Media.Color.FromRgb(Lighten(accent.R), Lighten(accent.G), Lighten(accent.B));
                Application.Current.Resources["Brush.PrimaryHover"] = new System.Windows.Media.SolidColorBrush(hover);
                var highlight = System.Windows.Media.Color.FromArgb(0x66, accent.R, accent.G, accent.B);
                var highlightBrush = new System.Windows.Media.SolidColorBrush(highlight);
                Application.Current.Resources["Brush.Highlight"] = highlightBrush;
                _adorner?.SetHighlightBrush(highlightBrush);
            }

            _adorner?.SetDimRanges([]);
            UpdateSyllableCounts();

            // Sidebar section visibility
            NotesSection.Visibility    = s.ShowNotes           ? Visibility.Visible : Visibility.Collapsed;
            NotesDivider.Visibility    = s.ShowNotes           ? Visibility.Visible : Visibility.Collapsed;
            PhoneticSection.Visibility = s.ShowPhoneticSection ? Visibility.Visible : Visibility.Collapsed;
            PhoneticDivider.Visibility = s.ShowPhoneticSection ? Visibility.Visible : Visibility.Collapsed;
            SemanticSection.Visibility = s.ShowSemanticSection ? Visibility.Visible : Visibility.Collapsed;

            // Debug labels
            if (!s.ShowDebugLabels)
            {
                FocusLabel.Visibility     = Visibility.Collapsed;
                HoverLabel.Visibility     = Visibility.Collapsed;
                LabelSeparator.Visibility = Visibility.Collapsed;
            }

            // Autosave timer
            if (_autoSaveTimer is not null && _autoSaveTickHandler is not null)
                _autoSaveTimer.Tick -= _autoSaveTickHandler;
            _autoSaveTimer?.Stop();
            if (s.AutoSave)
            {
                _autoSaveTimer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(s.AutoSaveIntervalSeconds)
                };
                _autoSaveTickHandler = (_, _) =>
                {
                    if (_isDirty && !string.IsNullOrEmpty(_filePath))
                        SaveFile(_filePath);
                };
                _autoSaveTimer.Tick += _autoSaveTickHandler;
                _autoSaveTimer.Start();
            }

            string word = GetActiveWord();
            if (!string.IsNullOrEmpty(word) && HighlightToggle.IsChecked == true)
                _adorner?.SetHighlights(FindWordRects(word));
        }

        private List<Rect> FindBracketRects()
        {
            var rects = new List<Rect>();

            foreach (var run in GetAllRuns())
            {
                string text = run.Text;
                foreach (System.Text.RegularExpressions.Match m in _allBrackets.Matches(text))
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

        private IEnumerable<Run> GetAllRuns() =>
            Editor.Document.Blocks
                .OfType<Paragraph>()
                .SelectMany(p => FlattenInlines(p.Inlines))
                .OfType<Run>();

        /// <summary>
        /// Returns bounding rects for every whole-word occurrence of <paramref name="word"/> in the document.
        /// Must be called on the UI thread.
        /// </summary>
        private List<Rect> FindWordRects(string word)
        {
            var rects = new List<Rect>();
            int len = word.Length;

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

        // Recursively flattensSpan nesting so LineBreak and Run elements are always at the top level.
        private static IEnumerable<Inline> FlattenInlines(InlineCollection inlines)
        {
            foreach (var inline in inlines)
            {
                if (inline is Span span)
                    foreach (var child in FlattenInlines(span.Inlines))
                        yield return child;
                else
                    yield return inline;
            }
        }

        // Splits a paragraph at its LineBreak elements, yielding one (start pointer, end pointer, text) per visual line.
        private static IEnumerable<(TextPointer Start, TextPointer End, string Text)> GetLineSegments(Paragraph para)
        {
            var segStart = para.ContentStart;
            var segEnd   = para.ContentStart;
            var sb = new System.Text.StringBuilder();
            bool nextRunSetsStart = false;

            foreach (var inline in FlattenInlines(para.Inlines))
            {
                if (inline is LineBreak)
                {
                    yield return (segStart, segEnd, sb.ToString());
                    sb.Clear();
                    nextRunSetsStart = true;
                }
                else if (inline is Run run)
                {
                    if (nextRunSetsStart)
                    {
                        segStart = run.ContentStart;
                        nextRunSetsStart = false;
                    }
                    segEnd = run.ContentEnd;
                    sb.Append(run.Text);
                }
            }

            yield return (segStart, segEnd, sb.ToString());
        }

        private static readonly (string Code, PartOfSpeech Pos)[] _posCodes =
        [
            ("n", PartOfSpeech.Noun),
            ("v", PartOfSpeech.Verb),
            ("a", PartOfSpeech.Adjective),
            ("s", PartOfSpeech.AdjectiveSatellite),
            ("r", PartOfSpeech.Adverb),
        ];

        private async void OnEditorSelectionChanged(object sender, RoutedEventArgs e)
        {
            _lookupCts.Cancel();
            _lookupCts.Dispose();
            _lookupCts = new CancellationTokenSource();
            var ct = _lookupCts.Token;

            try
            {
                await Task.Delay(_settings.LookupDebounceMs, ct);

                string word = GetActiveWord();

                FocusLabel.Text       = string.IsNullOrEmpty(word) ? "" : $"Focus: \"{word}\"";
                FocusLabel.Visibility = !string.IsNullOrEmpty(word) && _settings.ShowDebugLabels ? Visibility.Visible : Visibility.Collapsed;
                UpdateLabelSeparator();

                if (string.IsNullOrWhiteSpace(word))
                {
                    ClearSidebar();
                    return;
                }

                _adorner?.SetHighlights(
                    !_findBarOpen && HighlightToggle.IsChecked == true ? FindWordRects(word) : []);

                LookupSpinner.Visibility = Visibility.Visible;

                bool sortByZipf = _settings.SortByZipf;
                int limit = _settings.ResultLimit == 0 ? int.MaxValue : _settings.ResultLimit;
                var minWait = Task.Delay(500, ct);
                var resultTask = Task.Run(() =>
                {
                    static List<WordItem> ToItems(IEnumerable<string> words) =>
                        words.Select(w => new WordItem(w)).ToList();

                    var phonetics    = _db.GetPhonetics(word);
                    var rhymes       = ToItems(_db.GetRhymes(word, limit, sortByZipf).Select(r => r.Text));
                    var multi        = ToItems(_db.GetMultisyllabicRhymes(word, limit, sortByZipf).Select(r => r.Text));
                    var assonance    = ToItems(_db.GetAssonance(word, limit, sortByZipf).Select(r => r.Text));
                    var alliteration = ToItems(_db.GetAlliteration(word, limit, sortByZipf).Select(r => r.Text));
                    var synGroups    = _posCodes
                        .Select(p => new SynGroup(p.Pos, ToItems(_db.GetSynonyms(word, pos: p.Code, limit: limit, sortByZipf: sortByZipf).Select(r => r.Text))))
                        .Where(g => g.Words.Count > 0)
                        .ToList();
                    var antonyms  = ToItems(_db.GetAntonyms(word, limit, sortByZipf).Select(r => r.Text));
                    var hypernyms = ToItems(_db.GetHypernyms(word, null, limit, sortByZipf).Select(r => r.Text));
                    var hyponyms  = ToItems(_db.GetHyponyms(word, null, limit, sortByZipf).Select(r => r.Text));
                    return (phonetics, rhymes, multi, assonance, alliteration, synGroups, antonyms, hypernyms, hyponyms);
                }, ct);

                await Task.WhenAll(minWait, resultTask);
                ct.ThrowIfCancellationRequested();
                LookupSpinner.Visibility = Visibility.Collapsed;
                var result = resultTask.Result;

                // Word info strip
                var primary = result.phonetics.FirstOrDefault();
                if (primary is not null)
                {
                    WordLabel.Text     = word;
                    SyllableLabel.Text = $"{primary.SyllableCount} syl";
                    StressLabel.Text   = string.Join("-", primary.StressPattern.AsEnumerable());
                    ArpaLabel.Text     = primary.Arpa;
                    WordInfoStrip.Visibility   = Visibility.Visible;
                    WordInfoDivider.Visibility = Visibility.Visible;
                }
                else
                {
                    WordInfoStrip.Visibility   = Visibility.Collapsed;
                    WordInfoDivider.Visibility = Visibility.Collapsed;
                }

                // Phonetic groups
                SetWordGroup(RhymesExpander,        RhymesList,       RhymesCount,        result.rhymes);
                SetWordGroup(MultisyllabicExpander,  MultisyllabicList, MultisyllabicCount, result.multi);
                SetWordGroup(AssonanceExpander,     AssonanceList,    AssonanceCount,     result.assonance);
                SetWordGroup(AlliterationExpander,  AlliterationList, AlliterationCount,  result.alliteration);

                // Semantic groups
                var synTotal = result.synGroups.Sum(g => g.Words.Count);
                SynonymsList.ItemsSource    = result.synGroups;
                SynonymsCount.Text          = synTotal > 0 ? $" ({synTotal})" : "";
                SynonymsExpander.Visibility = result.synGroups.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

                SetWordGroup(AntonymsExpander,  AntonymsList,  AntonymsCount,  result.antonyms);
                SetWordGroup(HypernymsExpander, HypernymsList, HypernymsCount, result.hypernyms);
                SetWordGroup(HyponymsExpander,  HyponymsList,  HyponymsCount,  result.hyponyms);

                // Progressively enrich each item with its syllable count
                var allItems = result.rhymes
                    .Concat(result.multi)
                    .Concat(result.assonance)
                    .Concat(result.alliteration)
                    .Concat(result.synGroups.SelectMany(g => g.Words))
                    .Concat(result.antonyms)
                    .Concat(result.hypernyms)
                    .Concat(result.hyponyms)
                    .ToList();
                _ = EnrichWithSyllables(allItems, ct);

                ScrollToTypewriterPosition();
            }
            catch (OperationCanceledException)
            {
                LookupSpinner.Visibility = Visibility.Collapsed;
            }
        }

        private static void SetWordGroup(
            Expander expander, ItemsControl list, TextBlock count, IReadOnlyList<Models.WordItem> words)
        {
            list.ItemsSource    = words;
            count.Text          = words.Count > 0 ? $" ({words.Count})" : "";
            expander.Visibility = words.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private async Task EnrichWithSyllables(List<Models.WordItem> items, CancellationToken ct)
        {
            try
            {
                foreach (var item in items)
                {
                    var syl = await Task.Run(() => _db.GetSyllableCount(item.Word), ct);
                    if (syl.HasValue)
                        item.SyllableLabel = $"({syl.Value})";
                    await Task.Delay(15, ct);
                }
            }
            catch (OperationCanceledException) { }
        }

        private void OnSyllableToggled(object sender, RoutedEventArgs e)
        {
            if (SyllableToggle.IsChecked != true)
            {
                _adorner?.SetGutterEntries([]);
                return;
            }
            UpdateSyllableCounts();
        }

        private void OnHighlightToggled(object sender, RoutedEventArgs e)
        {
            if (HighlightToggle.IsChecked != true)
            {
                _adorner?.SetHighlights([]);
                return;
            }

            string word = GetActiveWord();
            if (!string.IsNullOrEmpty(word))
                _adorner?.SetHighlights(FindWordRects(word));
        }

        private void ClearSidebar()
        {
            FocusLabel.Visibility    = Visibility.Collapsed;
            UpdateLabelSeparator();
            LookupSpinner.Visibility = Visibility.Collapsed;
            _adorner?.SetHighlights([]);

            WordInfoStrip.Visibility   = Visibility.Collapsed;
            WordInfoDivider.Visibility = Visibility.Collapsed;

            RhymesExpander.Visibility        = Visibility.Collapsed;
            MultisyllabicExpander.Visibility = Visibility.Collapsed;
            AssonanceExpander.Visibility     = Visibility.Collapsed;
            AlliterationExpander.Visibility  = Visibility.Collapsed;

            SynonymsExpander.Visibility  = Visibility.Collapsed;
            AntonymsExpander.Visibility  = Visibility.Collapsed;
            HypernymsExpander.Visibility = Visibility.Collapsed;
            HyponymsExpander.Visibility  = Visibility.Collapsed;
        }

        private void ScrollToTypewriterPosition()
        {
            if (!_settings.TypewriterMode) return;
            var caretRect = Editor.CaretPosition?.GetCharacterRect(LogicalDirection.Forward) ?? Rect.Empty;
            if (caretRect.IsEmpty) return;
            var transform = Editor.TransformToAncestor(EditorCanvas);
            var caretCenter = transform.Transform(new Point(0, caretRect.Top + caretRect.Height / 2));
            EditorCanvas.ScrollToVerticalOffset(caretCenter.Y - EditorCanvas.ViewportHeight / 2);
        }

        private string GetActiveWord(){            TextSelection selection = Editor.Selection;

            if (!selection.IsEmpty)
            {
                string raw = selection.Text.Trim().Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
                return new string(raw.Where(c => char.IsLetterOrDigit(c) || c == '\'').ToArray());
            }

            TextPointer caret = selection.Start;

            TextPointer start = caret;
            while (true)
            {
                TextPointer prev = start.GetPositionAtOffset(-1);
                if (prev == null) break;
                string ch = new TextRange(prev, start).Text;
                if (ch.Length == 0 || char.IsWhiteSpace(ch[0])) break;
                start = prev;
            }

            TextPointer end = caret;
            while (true)
            {
                TextPointer next = end.GetPositionAtOffset(1);
                if (next == null) break;
                string ch = new TextRange(end, next).Text;
                if (ch.Length == 0 || char.IsWhiteSpace(ch[0])) break;
                end = next;
            }

            return new string(new TextRange(start, end).Text.Trim().Where(c => char.IsLetterOrDigit(c) || c == '\'').ToArray());
        }

        private void OnEditorMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (HoverToggle.IsChecked != true) return;
            _hoverPoint = e.GetPosition(Editor);
            _hoverTimer.Stop();
            _hoverTimer.Start();
        }

        private void OnHoverTimerTick(object? sender, EventArgs e)
        {
            _hoverTimer.Stop();

            var pointer = Editor.GetPositionFromPoint(_hoverPoint, snapToText: false);
            if (pointer == null)
            {
                ClearHoverLabel();
                return;
            }

            TextPointer start = pointer;
            while (true)
            {
                TextPointer prev = start.GetPositionAtOffset(-1);
                if (prev == null) break;
                string ch = new TextRange(prev, start).Text;
                if (ch.Length == 0 || char.IsWhiteSpace(ch[0])) break;
                start = prev;
            }

            TextPointer end = pointer;
            while (true)
            {
                TextPointer next = end.GetPositionAtOffset(1);
                if (next == null) break;
                string ch = new TextRange(end, next).Text;
                if (ch.Length == 0 || char.IsWhiteSpace(ch[0])) break;
                end = next;
            }

            string word = new string(new TextRange(start, end).Text.Trim()
                .Where(c => char.IsLetterOrDigit(c) || c == '\'').ToArray());

            if (string.IsNullOrEmpty(word))
            {
                ClearHoverLabel();
                return;
            }

            HoverLabel.Text       = $"Hover: \"{word}\"";
            HoverLabel.Visibility = _settings.ShowDebugLabels ? Visibility.Visible : Visibility.Collapsed;
            UpdateLabelSeparator();

            // If the same word is already showing, leave the popup where it is.
            if (word == _hoverWord && HoverPopup.IsOpen) return;

            _hoverWord = word;

            // Word changed — reposition by closing and reopening (Placement="Mouse" snaps on open).
            HoverPopup.IsOpen = false;

            // Populate and show the popup
            var phonetics = _db.GetPhonetics(word);
            var primary   = phonetics.FirstOrDefault();

            PopupWordLabel.Text = word;
            if (primary is not null)
            {
                string stress = string.Join("-", primary.StressPattern.AsEnumerable());
                PopupMetaLabel.Text       = $"{primary.SyllableCount} syl  ·  {stress}  ·  {primary.Arpa}";
                PopupMetaLabel.Visibility = Visibility.Visible;
            }
            else
            {
                PopupMetaLabel.Visibility = Visibility.Collapsed;
            }

            HoverPopup.IsOpen = true;
        }

        private void OnEditorMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _hoverTimer.Stop();
            ClearHoverLabel();
        }

        private void ClearHoverLabel()
        {
            HoverLabel.Visibility     = Visibility.Collapsed;
            LabelSeparator.Visibility = Visibility.Collapsed;
            HoverPopup.IsOpen         = false;
            _hoverWord                = string.Empty;
        }

        private void UpdateLabelSeparator()
        {
            LabelSeparator.Visibility = HoverLabel.Visibility == Visibility.Visible
                                     && FocusLabel.Visibility == Visibility.Visible
                                        ? Visibility.Visible
                                        : Visibility.Collapsed;
        }
    }
}
