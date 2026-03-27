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

        Result = new AppSettings
        {
            HighlightColor = selectedColor,
            DimBrackets    = DimBracketsCheck.IsChecked == true,
            DimSquare      = DimSquareCheck.IsChecked   == true,
            DimRound       = DimRoundCheck.IsChecked    == true,
            DimCurly       = DimCurlyCheck.IsChecked    == true,
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
