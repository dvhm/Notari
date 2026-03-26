using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Notari.Models;

namespace Notari
{
    public partial class MainWindow : Window
    {
        private void OnEditorTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateDocumentStats();
            SetDirty(true);
            UpdateSyllableCounts();
        }

        private void UpdateDocumentStats()
        {
            var text = new TextRange(Editor.Document.ContentStart, Editor.Document.ContentEnd).Text;
            _wordCount = text.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries).Length;
            _lineCount = Editor.Document.Blocks.Count;
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
            _syllableCts.Cancel();
            _syllableCts.Dispose();
            _syllableCts = new CancellationTokenSource();
            var ct = _syllableCts.Token;

            try
            {
                await Task.Delay(500, ct);

                // Collect paragraph positions and text on the UI thread.
                var paragraphs = Editor.Document.Blocks
                    .OfType<Paragraph>()
                    .Select(p =>
                    {
                        var rect = p.ContentStart.GetCharacterRect(LogicalDirection.Forward);
                        var text = new TextRange(p.ContentStart, p.ContentEnd).Text;
                        return (Y: rect.Top, Text: text, Valid: !rect.IsEmpty);
                    })
                    .Where(p => p.Valid)
                    .ToList();

                // Compute syllable counts on a background thread.
                var entries = await Task.Run(() =>
                    paragraphs
                        .Select(p =>
                        {
                            var words = p.Text
                                .Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
                                .Select(w => new string(w.Where(c => char.IsLetterOrDigit(c) || c == '\'').ToArray()))
                                .Where(w => w.Length > 0);
                            int syl = words.Sum(w => _db.GetSyllableCount(w) ?? 0);
                            return (p.Y, Syl: syl);
                        })
                        .Where(e => e.Syl > 0)
                        .ToList()
                , ct);

                _adorner?.SetGutterEntries(entries);
            }
            catch (OperationCanceledException) { }
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
                await Task.Delay(250, ct);

                string word = GetActiveWord();

                FocusLabel.Text       = string.IsNullOrEmpty(word) ? "" : $"Focus: \"{word}\"";
                FocusLabel.Visibility = string.IsNullOrEmpty(word) ? Visibility.Collapsed : Visibility.Visible;

                if (string.IsNullOrWhiteSpace(word))
                {
                    ClearSidebar();
                    return;
                }

                var result = await Task.Run(() =>
                {
                    var phonetics    = _db.GetPhonetics(word);
                    var rhymes       = _db.GetRhymes(word).Select(r => r.Text).ToList();
                    var multi        = _db.GetMultisyllabicRhymes(word).Select(r => r.Text).ToList();
                    var assonance    = _db.GetAssonance(word).Select(r => r.Text).ToList();
                    var alliteration = _db.GetAlliteration(word).Select(r => r.Text).ToList();
                    var synGroups    = _posCodes
                        .Select(p => new SynGroup(p.Pos, _db.GetSynonyms(word, pos: p.Code).Select(r => r.Text).ToList()))
                        .Where(g => g.Words.Count > 0)
                        .ToList();
                    var antonyms  = _db.GetAntonyms(word).Select(r => r.Text).ToList();
                    var hypernyms = _db.GetHypernyms(word).Select(r => r.Text).ToList();
                    var hyponyms  = _db.GetHyponyms(word).Select(r => r.Text).ToList();
                    return (phonetics, rhymes, multi, assonance, alliteration, synGroups, antonyms, hypernyms, hyponyms);
                }, ct);

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
            }
            catch (OperationCanceledException) { }
        }

        private static void SetWordGroup(
            Expander expander, ItemsControl list, TextBlock count, IReadOnlyList<string> words)
        {
            list.ItemsSource    = words;
            count.Text          = words.Count > 0 ? $" ({words.Count})" : "";
            expander.Visibility = words.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ClearSidebar()
        {
            FocusLabel.Visibility = Visibility.Collapsed;

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

        private string GetActiveWord()
        {
            TextSelection selection = Editor.Selection;

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
    }
}
