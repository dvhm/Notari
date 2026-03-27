using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace Notari.Dialogs;

public partial class SettingsDialog : Window
{
    private readonly Dictionary<RadioButton, string> _accentMap;
    private readonly AppSettings _original;

    public AppSettings Result { get; private set; }

    public SettingsDialog(Window owner, AppSettings current)
    {
        InitializeComponent();
        Owner = owner;
        Result = current;
        _original = current;

        _accentMap = new()
        {
            { AccentBlue,   "#FF4FC3F7" },
            { AccentPurple, "#FFB07EFC" },
            { AccentPink,   "#FFFC7EC8" },
            { AccentRed,    "#FFEF5350" },
            { AccentOrange, "#FFFFA040" },
            { AccentYellow, "#FFFFC107" },
            { AccentGreen,  "#FF4CAF50" },
            { AccentCyan,   "#FF26C6DA" },
        };

        bool found = false;
        foreach (var (btn, color) in _accentMap)
        {
            if (color.Equals(current.AccentColor, StringComparison.OrdinalIgnoreCase))
            {
                btn.IsChecked = true;
                found = true;
                break;
            }
        }
        if (!found) AccentBlue.IsChecked = true;

        DimBracketsCheck.IsChecked = current.DimBrackets;
        DimSquareCheck.IsChecked   = current.DimSquare;
        DimRoundCheck.IsChecked    = current.DimRound;
        DimCurlyCheck.IsChecked    = current.DimCurly;

        TypewriterModeCheck.IsChecked = current.TypewriterMode;

        ShowNotesCheck.IsChecked    = current.ShowNotes;
        ShowPhoneticCheck.IsChecked = current.ShowPhoneticSection;
        ShowSemanticCheck.IsChecked = current.ShowSemanticSection;

        SortByZipfCheck.IsChecked = current.SortByZipf;

        foreach (ComboBoxItem item in ResultLimitBox.Items)
            if (int.Parse((string)item.Tag) == current.ResultLimit) { ResultLimitBox.SelectedItem = item; break; }
        if (ResultLimitBox.SelectedItem is null) ResultLimitBox.SelectedIndex = 3;

        foreach (ComboBoxItem item in DebounceBox.Items)
            if (int.Parse((string)item.Tag) == current.LookupDebounceMs) { DebounceBox.SelectedItem = item; break; }
        if (DebounceBox.SelectedItem is null) DebounceBox.SelectedIndex = 1;

        AutoSaveCheck.IsChecked = current.AutoSave;

        foreach (ComboBoxItem item in AutoSaveIntervalBox.Items)
            if (int.Parse((string)item.Tag) == current.AutoSaveIntervalSeconds) { AutoSaveIntervalBox.SelectedItem = item; break; }
        if (AutoSaveIntervalBox.SelectedItem is null) AutoSaveIntervalBox.SelectedIndex = 2;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        NativeWindowHelper.ApplyRoundedCorners(new WindowInteropHelper(this).Handle);
    }

    private void OnTitleBarClose(object sender, RoutedEventArgs e)
    {
        if (!HasChanges()) { OnCancel(sender, e); return; }

        var dlg = new UnsavedSettingsDialog(this);
        dlg.ShowDialog();
        if (dlg.Save)
            OnOk(sender, e);
        else if (dlg.Discard)
            OnCancel(sender, e);
        // Cancel: do nothing, stay in settings
    }

    private bool HasChanges()
    {
        var accent = _accentMap.FirstOrDefault(kv => kv.Key.IsChecked == true).Value ?? "#FF4FC3F7";
        var resultLimit    = ResultLimitBox.SelectedItem    is ComboBoxItem rli ? int.Parse((string)rli.Tag) : 0;
        var debounceMs     = DebounceBox.SelectedItem       is ComboBoxItem dbi ? int.Parse((string)dbi.Tag) : 250;
        var autoSaveInt    = AutoSaveIntervalBox.SelectedItem is ComboBoxItem asi ? int.Parse((string)asi.Tag) : 300;

        return !accent.Equals(_original.AccentColor, StringComparison.OrdinalIgnoreCase)
            || DimBracketsCheck.IsChecked  != _original.DimBrackets
            || DimSquareCheck.IsChecked    != _original.DimSquare
            || DimRoundCheck.IsChecked     != _original.DimRound
            || DimCurlyCheck.IsChecked     != _original.DimCurly
            || TypewriterModeCheck.IsChecked != _original.TypewriterMode
            || ShowNotesCheck.IsChecked    != _original.ShowNotes
            || ShowPhoneticCheck.IsChecked != _original.ShowPhoneticSection
            || ShowSemanticCheck.IsChecked != _original.ShowSemanticSection
            || SortByZipfCheck.IsChecked   != _original.SortByZipf
            || resultLimit                 != _original.ResultLimit
            || debounceMs                  != _original.LookupDebounceMs
            || AutoSaveCheck.IsChecked     != _original.AutoSave
            || autoSaveInt                 != _original.AutoSaveIntervalSeconds;
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        var selectedAccent = _accentMap
            .FirstOrDefault(kv => kv.Key.IsChecked == true).Value ?? "#FF4FC3F7";

        var resultLimit = ResultLimitBox.SelectedItem is ComboBoxItem rli
            ? int.Parse((string)rli.Tag) : 0;
        var debounceMs = DebounceBox.SelectedItem is ComboBoxItem dbi
            ? int.Parse((string)dbi.Tag) : 250;
        var autoSaveInterval = AutoSaveIntervalBox.SelectedItem is ComboBoxItem asi
            ? int.Parse((string)asi.Tag) : 300;

        Result = new AppSettings
        {
            AccentColor             = selectedAccent,
            DimBrackets             = DimBracketsCheck.IsChecked == true,
            DimSquare               = DimSquareCheck.IsChecked   == true,
            DimRound                = DimRoundCheck.IsChecked    == true,
            DimCurly                = DimCurlyCheck.IsChecked    == true,
            SortByZipf              = SortByZipfCheck.IsChecked  == true,
            ResultLimit             = resultLimit,
            AutoSave                = AutoSaveCheck.IsChecked    == true,
            AutoSaveIntervalSeconds = autoSaveInterval,
            ShowNotes               = ShowNotesCheck.IsChecked   == true,
            ShowPhoneticSection     = ShowPhoneticCheck.IsChecked == true,
            ShowSemanticSection     = ShowSemanticCheck.IsChecked == true,
            LookupDebounceMs        = debounceMs,
            TypewriterMode          = TypewriterModeCheck.IsChecked == true,
        };
        DialogResult = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
