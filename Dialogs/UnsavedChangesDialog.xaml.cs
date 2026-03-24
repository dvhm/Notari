using System.Windows;

namespace Notari.Dialogs;

public partial class UnsavedChangesDialog : Window
{
    public bool Discard { get; private set; }

    public UnsavedChangesDialog(Window owner)
    {
        InitializeComponent();
        Owner = owner;
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
