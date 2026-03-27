using System.Windows;
using System.Windows.Interop;

namespace Notari.Dialogs;

public partial class UnsavedChangesDialog : Window
{
    public bool Discard { get; private set; }

    public UnsavedChangesDialog(Window owner)
    {
        InitializeComponent();
        Owner = owner;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        NativeWindowHelper.ApplyRoundedCorners(new WindowInteropHelper(this).Handle);
    }

    private void OnDiscard(object sender, RoutedEventArgs e)
    {
        Discard = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        Discard = false;
        Close();
    }
}
