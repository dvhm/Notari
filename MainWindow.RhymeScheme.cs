using PhonKit;
using System.Windows;
using System.Windows.Documents;

namespace Notari
{
    public partial class MainWindow : Window
    {
        private CancellationTokenSource _rhymeSchemeCts = new();

        private void OnRhymeSchemeToggled(object sender, RoutedEventArgs e)
        {
            if (RhymeSchemeToggle.IsChecked != true)
            {
                _adorner?.SetRhymeLabels([]);
                return;
            }
            UpdateRhymeScheme();
        }

        private void UpdateRhymeScheme()
        {
            if (RhymeSchemeToggle?.IsChecked != true) return;
            _ = RunRhymeSchemeAsync();
        }

        private async Task RunRhymeSchemeAsync()
        {
            _rhymeSchemeCts.Cancel();
            _rhymeSchemeCts.Dispose();
            _rhymeSchemeCts = new CancellationTokenSource();
            var ct = _rhymeSchemeCts.Token;

            try
            {
                await Task.Delay(500, ct);

                // Collect (Y, X, lastWord) for every logical line segment on the UI thread.
                var segments = Editor.Document.Blocks
                    .OfType<Paragraph>()
                    .SelectMany(GetLineSegments)
                    .Select(s =>
                    {
                        var startRect = s.Start.GetCharacterRect(LogicalDirection.Forward);
                        var endRect   = s.End.GetCharacterRect(LogicalDirection.Backward);
                        return (Y: startRect.Top, X: endRect.Right, LastWord: GetLastWordOf(s.Text), Valid: !startRect.IsEmpty);
                    })
                    .Where(s => s.Valid)
                    .ToList();

                var labeled = await Task.Run(() =>
                {
                    // Split at blank lines (stanza breaks) and label each stanza independently.
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

                _adorner?.SetRhymeLabels(labeled);
            }
            catch (OperationCanceledException) { }
        }

        private static string GetLastWordOf(string text)
        {
            var trimmed = text.TrimStart();
            if (trimmed.Length > 0 && trimmed[0] is '[' or '(' or '{')
                return string.Empty;

            var parts = trimmed.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
            for (int i = parts.Length - 1; i >= 0; i--)
            {
                string cleaned = new string(parts[i].Where(c => char.IsLetterOrDigit(c) || c == '\'').ToArray());
                if (cleaned.Length > 0) return cleaned.ToLowerInvariant();
            }
            return string.Empty;
        }

        /// <summary>
        /// Assigns rhyme-scheme letters (A, B, C…) to each line within a stanza.
        /// Compares each end-word against the representative of every existing group using
        /// <see cref="PhoneticDatabase.GetRhymeScore"/>; any score above None (including
        /// assonance) counts as a match. Lines whose end-word is unknown to the database are omitted.
        /// </summary>
        private IReadOnlyList<(double Y, double X, string Label)> AssignRhymeLetters(
            List<(double Y, double X, string Word)> lines)
        {
            var groups = new List<(char Letter, string Representative)>();
            char nextLetter = 'A';
            var result = new List<(double Y, double X, string Label)>();

            foreach (var (y, x, word) in lines)
            {
                int matchIdx = -1;
                for (int i = 0; i < groups.Count; i++)
                {
                    if (_db.GetRhymeScore(word, groups[i].Representative) != RhymeScore.None)
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
    }
}

