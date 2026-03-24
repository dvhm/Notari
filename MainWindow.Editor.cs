using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Notari.Models;

namespace Notari
{
    public partial class MainWindow : Window
    {
        private void OnEditorTextChanged(object sender, TextChangedEventArgs e) => SetDirty(true);

        private void OnEditorSelectionChanged(object sender, RoutedEventArgs e)
        {
            string word = GetActiveWord();
            if (string.IsNullOrWhiteSpace(word))
                return;

            var copies = Enumerable.Repeat(word, 10).ToList();
            RhymesList.ItemsSource = copies;

            var groups = _synsetEngine.GetSynonyms(word)
                .SelectMany(syn => _synsetEngine.GetWordPOS(syn)
                    .Select(pos => new SynResult(word, syn, ParsePOS(pos))))
                .GroupBy(r => r.Pos)
                .OrderBy(g => (int)g.Key)
                .Select(g => new SynGroup(g.Key, g.Select(r => r.Synonym).Distinct().ToList()))
                .ToList();

            SynonymsList.ItemsSource = groups;
        }

        private static PartOfSpeech ParsePOS(string pos) => pos switch
        {
            "v" => PartOfSpeech.Verb,
            "a" => PartOfSpeech.Adjective,
            "s" => PartOfSpeech.AdjectiveSatellite,
            "r" => PartOfSpeech.Adverb,
            _   => PartOfSpeech.Noun,
        };

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
