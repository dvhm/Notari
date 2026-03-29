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
            { AccentBlue,   GetSwatchHex("Color.Swatch.Blue")   },
            { AccentPurple, GetSwatchHex("Color.Swatch.Purple") },
            { AccentPink,   GetSwatchHex("Color.Swatch.Pink")   },
            { AccentRed,    GetSwatchHex("Color.Swatch.Red")    },
            { AccentOrange, GetSwatchHex("Color.Swatch.Orange") },
            { AccentYellow, GetSwatchHex("Color.Swatch.Yellow") },
            { AccentGreen,  GetSwatchHex("Color.Swatch.Green")  },
            { AccentCyan,   GetSwatchHex("Color.Swatch.Cyan")   },
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

        SelectByTag(ResultLimitBox,      current.ResultLimit,             fallbackIndex: 3);
        SelectByTag(DebounceBox,          current.LookupDebounceMs,        fallbackIndex: 1);

        AutoSaveCheck.IsChecked = current.AutoSave;
        SelectByTag(AutoSaveIntervalBox,  current.AutoSaveIntervalSeconds, fallbackIndex: 2);

        ShowDebugLabelsCheck.IsChecked = current.ShowDebugLabels;
        SelectByTagDouble(ScreenshotScaleBox, current.ScreenshotScale, fallbackIndex: 2);
    }

    private static string GetSwatchHex(string colorKey) =>
        ((System.Windows.Media.Color)System.Windows.Application.Current.Resources[colorKey]).ToString();

    private static void SelectByTag(ComboBox box, int value, int fallbackIndex)
    {
        foreach (ComboBoxItem item in box.Items)
            if (item.Tag is string s && int.TryParse(s, out int v) && v == value)
            { box.SelectedItem = item; return; }
        box.SelectedIndex = fallbackIndex;
    }

    private static int GetTag(ComboBox box, int fallback) =>
        box.SelectedItem is ComboBoxItem item && int.TryParse(item.Tag as string, out int v) ? v : fallback;

    private static void SelectByTagDouble(ComboBox box, double value, int fallbackIndex)
    {
        foreach (ComboBoxItem item in box.Items)
            if (item.Tag is string s && double.TryParse(s, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double v) && Math.Abs(v - value) < 0.001)
            { box.SelectedItem = item; return; }
        box.SelectedIndex = fallbackIndex;
    }

    private static double GetTagDouble(ComboBox box, double fallback) =>
        box.SelectedItem is ComboBoxItem item &&
        double.TryParse(item.Tag as string, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out double v)
        ? v : fallback;

    protected override void OnSourceInitialized(EventArgs e){
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
        var accent       = _accentMap.FirstOrDefault(kv => kv.Key.IsChecked == true).Value ?? GetSwatchHex("Color.Swatch.Blue");
        var resultLimit  = GetTag(ResultLimitBox,     fallback: 0);
        var debounceMs   = GetTag(DebounceBox,         fallback: 250);
        var autoSaveInt  = GetTag(AutoSaveIntervalBox, fallback: 300);

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
            || autoSaveInt                 != _original.AutoSaveIntervalSeconds
            || ShowDebugLabelsCheck.IsChecked != _original.ShowDebugLabels
            || Math.Abs(GetTagDouble(ScreenshotScaleBox, 2.0) - _original.ScreenshotScale) > 0.001;
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        var selectedAccent   = _accentMap.FirstOrDefault(kv => kv.Key.IsChecked == true).Value ?? GetSwatchHex("Color.Swatch.Blue");
        var resultLimit      = GetTag(ResultLimitBox,     fallback: 0);
        var debounceMs       = GetTag(DebounceBox,         fallback: 250);
        var autoSaveInterval = GetTag(AutoSaveIntervalBox, fallback: 300);

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
            ShowDebugLabels         = ShowDebugLabelsCheck.IsChecked == true,
            ScreenshotScale         = GetTagDouble(ScreenshotScaleBox, fallback: 2.0),
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
