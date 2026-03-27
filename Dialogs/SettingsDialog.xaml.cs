using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace Notari.Dialogs;

public partial class SettingsDialog : Window
{
    private readonly Dictionary<RadioButton, string> _swatchMap;

    public AppSettings Result { get; private set; } = new();

    public SettingsDialog(Window owner, AppSettings current)
    {
        InitializeComponent();
        Owner = owner;
        Result = current;

        _swatchMap = new()
        {
            { SwatchPurple,  "#66C084FC" },
            { SwatchPink,    "#66FC84E0" },
            { SwatchRed,     "#66FC8484" },
            { SwatchOrange,  "#66FCBE84" },
            { SwatchYellow,  "#66FCE884" },
            { SwatchGreen,   "#6684FC98" },
            { SwatchCyan,    "#6684ECFC" },
            { SwatchNeutral, "#66AAAAAA" },
        };

        bool found = false;
        foreach (var (btn, color) in _swatchMap)
        {
            if (color.Equals(current.HighlightColor, StringComparison.OrdinalIgnoreCase))
            {
                btn.IsChecked = true;
                found = true;
                break;
            }
        }
        if (!found) SwatchPurple.IsChecked = true;

        DimBracketsCheck.IsChecked = current.DimBrackets;
        DimSquareCheck.IsChecked   = current.DimSquare;
        DimRoundCheck.IsChecked    = current.DimRound;
        DimCurlyCheck.IsChecked    = current.DimCurly;

        SpellCheckBox.IsChecked       = current.SpellCheck;
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

    private void OnOk(object sender, RoutedEventArgs e)
    {
        var selectedColor = _swatchMap
            .FirstOrDefault(kv => kv.Key.IsChecked == true).Value ?? "#66C084FC";

        var resultLimit = ResultLimitBox.SelectedItem is ComboBoxItem rli
            ? int.Parse((string)rli.Tag) : 0;
        var debounceMs = DebounceBox.SelectedItem is ComboBoxItem dbi
            ? int.Parse((string)dbi.Tag) : 250;
        var autoSaveInterval = AutoSaveIntervalBox.SelectedItem is ComboBoxItem asi
            ? int.Parse((string)asi.Tag) : 300;

        Result = new AppSettings
        {
            HighlightColor          = selectedColor,
            DimBrackets             = DimBracketsCheck.IsChecked == true,
            DimSquare               = DimSquareCheck.IsChecked   == true,
            DimRound                = DimRoundCheck.IsChecked    == true,
            DimCurly                = DimCurlyCheck.IsChecked    == true,
            SortByZipf              = SortByZipfCheck.IsChecked  == true,
            ResultLimit             = resultLimit,
            SpellCheck              = SpellCheckBox.IsChecked    == true,
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
