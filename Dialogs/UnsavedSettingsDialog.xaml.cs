using System.Windows;
using System.Windows.Interop;

namespace Notari.Dialogs;

public partial class UnsavedSettingsDialog : Window
{
    public bool Save    { get; private set; }
    public bool Discard { get; private set; }

    public UnsavedSettingsDialog(Window owner)
    {
        InitializeComponent();
        Owner = owner;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        NativeWindowHelper.ApplyRoundedCorners(new WindowInteropHelper(this).Handle);
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        Save = true;
        Close();
    }

    private void OnDiscard(object sender, RoutedEventArgs e)
    {
        Discard = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
