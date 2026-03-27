using System.Windows;
using System.Windows.Interop;

namespace Notari.Dialogs;

public partial class StarMessageDialog : Window
{
    public StarMessageDialog(Window owner)
    {
        InitializeComponent();
        Owner = owner;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        NativeWindowHelper.ApplyRoundedCorners(new WindowInteropHelper(this).Handle);
    }

    private void OnOk(object sender, RoutedEventArgs e) => Close();
}
