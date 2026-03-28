using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Notari.Models;
using PhonKit;

namespace Notari
{
    public partial class MainWindow : Window
    {
        private readonly System.Windows.Threading.DispatcherTimer _statsDebounce = new()
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };

        private Regex _allBrackets = new(@"(?!x)x", RegexOptions.Compiled);

        private void OnEditorTextChanged(object sender, TextChangedEventArgs e)
        {
            _statsDebounce.Stop();
            _statsDebounce.Start();
            SetDirty(true);
            _adornerService.ClearOverlays();
            if (_vm is not null && SyllableToggle?.IsChecked == true)
                _ = _vm.UpdateSyllableCountsAsync(_settings);
            if (_vm is not null && RhymeSchemeToggle?.IsChecked == true)
                _ = _vm.UpdateRhymeSchemeAsync();
            if (_settings.TypewriterMode)
                _docService?.ScrollToTypewriterPosition(EditorCanvas);
        }

        internal void ApplySettings(AppSettings s, bool save = true)
        {
            _settings = s;
            if (save) _ = s.SaveAsync();

            _allBrackets = _textAnalysis.BuildBracketRegex(s);

            if (System.Windows.Media.ColorConverter.ConvertFromString(s.AccentColor) is System.Windows.Media.Color accent)
            {
                Application.Current.Resources["Brush.Primary"] = new System.Windows.Media.SolidColorBrush(accent);
                static byte Lighten(byte ch) => (byte)Math.Min(255, ch + (255 - ch) / 4);
                var hover = System.Windows.Media.Color.FromRgb(Lighten(accent.R), Lighten(accent.G), Lighten(accent.B));
                Application.Current.Resources["Brush.PrimaryHover"] = new System.Windows.Media.SolidColorBrush(hover);
                var highlight      = System.Windows.Media.Color.FromArgb(0x66, accent.R, accent.G, accent.B);
                var highlightBrush = new System.Windows.Media.SolidColorBrush(highlight);
                Application.Current.Resources["Brush.Highlight"] = highlightBrush;
                _adornerService.SetHighlightBrush(highlightBrush);
            }

            _adornerService.SetDimRanges([]);
            if (SyllableToggle?.IsChecked == true)
                _ = _vm?.UpdateSyllableCountsAsync(s);

            NotesSection.Visibility    = s.ShowNotes           ? Visibility.Visible : Visibility.Collapsed;
            NotesDivider.Visibility    = s.ShowNotes           ? Visibility.Visible : Visibility.Collapsed;
            PhoneticSection.Visibility = s.ShowPhoneticSection ? Visibility.Visible : Visibility.Collapsed;
            PhoneticDivider.Visibility = s.ShowPhoneticSection ? Visibility.Visible : Visibility.Collapsed;
            SemanticSection.Visibility = s.ShowSemanticSection ? Visibility.Visible : Visibility.Collapsed;

            if (!s.ShowDebugLabels)
            {
                FocusLabel.Visibility     = Visibility.Collapsed;
                HoverLabel.Visibility     = Visibility.Collapsed;
                LabelSeparator.Visibility = Visibility.Collapsed;
            }

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

            if (_docService is not null)
            {
                string word = _docService.GetActiveWord();
                if (!string.IsNullOrEmpty(word) && HighlightToggle.IsChecked == true)
                    _adornerService.SetHighlights(_docService.FindWordRects(word));
            }
        }

        private async void OnEditorSelectionChanged(object sender, RoutedEventArgs e)
        {
            if (_vm is null) return;
            FocusLabel.Visibility = Visibility.Collapsed;
            UpdateLabelSeparator();
            await _vm.OnSelectionChangedAsync(_settings, HighlightToggle.IsChecked == true, _findBarOpen);
        }

        private void OnSyllableToggled(object sender, RoutedEventArgs e)
        {
            if (SyllableToggle.IsChecked != true)
            {
                _adornerService.SetGutterEntries([]);
                return;
            }
            _ = _vm.UpdateSyllableCountsAsync(_settings);
        }

        private void OnHighlightToggled(object sender, RoutedEventArgs e)
        {
            if (HighlightToggle.IsChecked != true)
            {
                _adornerService.SetHighlights([]);
                return;
            }
            string word = _docService.GetActiveWord();
            if (!string.IsNullOrEmpty(word))
                _adornerService.SetHighlights(_docService.FindWordRects(word));
        }

        private void ClearSidebar()
        {
            FocusLabel.Visibility    = Visibility.Collapsed;
            UpdateLabelSeparator();
            LookupSpinner.Visibility = Visibility.Collapsed;
            _adornerService.SetHighlights([]);

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

        private void OnEditorMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (HoverToggle.IsChecked != true) return;
            _hoverPoint = e.GetPosition(Editor);
            _hoverTimer.Stop();
            _hoverTimer.Start();
        }

        private async void OnHoverTimerTick(object? sender, EventArgs e)
        {
            _hoverTimer.Stop();

            var pointer = Editor.GetPositionFromPoint(_hoverPoint, snapToText: false);
            if (pointer == null) { ClearHoverUI(); return; }

            string word = _docService.GetWordAtPointer(pointer);
            if (string.IsNullOrEmpty(word)) { ClearHoverUI(); return; }

            HoverLabel.Text       = $"Hover: \"{word}\"";
            HoverLabel.Visibility = _settings.ShowDebugLabels ? Visibility.Visible : Visibility.Collapsed;
            UpdateLabelSeparator();

            if (word == _vm.HoverWord && HoverPopup.IsOpen) return;

            HoverPopup.IsOpen = false;
            await _vm.OnHoverWordAsync(word);
        }

        private void OnEditorMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _hoverTimer.Stop();
            ClearHoverUI();
            _vm.ClearHover();
        }

        private void ClearHoverUI()
        {
            HoverLabel.Visibility     = Visibility.Collapsed;
            LabelSeparator.Visibility = Visibility.Collapsed;
            HoverPopup.IsOpen         = false;
        }

        private void UpdateLabelSeparator()
        {
            LabelSeparator.Visibility = HoverLabel.Visibility == Visibility.Visible
                                     && FocusLabel.Visibility == Visibility.Visible
                                        ? Visibility.Visible
                                        : Visibility.Collapsed;
        }

        private static void SetWordGroup(
            Expander expander, ItemsControl list, TextBlock count, IReadOnlyList<WordItem> words)
        {
            list.ItemsSource    = words;
            count.Text          = words.Count > 0 ? $" ({words.Count})" : "";
            expander.Visibility = words.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        // ── ViewModel event handlers ─────────────────────────────────────────

        private void OnLookupCleared() => ClearSidebar();

        private void OnLookupCompleted(LookupResult result)
        {
            string word = result.Word;
            FocusLabel.Text       = $"Focus: \"{word}\"";
            FocusLabel.Visibility = _settings.ShowDebugLabels ? Visibility.Visible : Visibility.Collapsed;
            UpdateLabelSeparator();

            var primary = result.Phonetics.FirstOrDefault();
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

            SetWordGroup(RhymesExpander,        RhymesList,        RhymesCount,        result.Rhymes);
            SetWordGroup(MultisyllabicExpander,  MultisyllabicList, MultisyllabicCount, result.MultisyllabicRhymes);
            SetWordGroup(AssonanceExpander,      AssonanceList,     AssonanceCount,     result.Assonance);
            SetWordGroup(AlliterationExpander,   AlliterationList,  AlliterationCount,  result.Alliteration);

            var synTotal = result.SynonymGroups.Sum(g => g.Words.Count);
            SynonymsList.ItemsSource    = result.SynonymGroups;
            SynonymsCount.Text          = synTotal > 0 ? $" ({synTotal})" : "";
            SynonymsExpander.Visibility = result.SynonymGroups.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            SetWordGroup(AntonymsExpander,  AntonymsList,  AntonymsCount,  result.Antonyms);
            SetWordGroup(HypernymsExpander, HypernymsList, HypernymsCount, result.Hypernyms);
            SetWordGroup(HyponymsExpander,  HyponymsList,  HyponymsCount,  result.Hyponyms);

            if (_settings.TypewriterMode)
                _docService.ScrollToTypewriterPosition(EditorCanvas);
        }

        private void OnSyllableCountsReady(IReadOnlyList<(double Y, int Syl)> entries)
        {
            _adornerService.SetGutterEntries(entries);
            if (_settings.DimBrackets)
                _adornerService.SetDimRanges(_docService.FindBracketRects(_allBrackets));
            else
                _adornerService.SetDimRanges([]);
        }

        private void OnRhymeSchemeReady(IReadOnlyList<(double Y, double X, string Label)> labels) =>
            _adornerService.SetRhymeLabels(labels);

        private void OnHoverReady(string word, PhoneticInfo? primary)
        {
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

        private void OnHoverCleared() => HoverPopup.IsOpen = false;
    }
}
