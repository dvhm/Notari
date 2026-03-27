using System.Windows;

namespace Notari.Dialogs;

public partial class StarMessageDialog : Window
{
    public StarMessageDialog(Window owner)
    {
        InitializeComponent();
        Owner = owner;
    }

    private void OnOk(object sender, RoutedEventArgs e) => Close();
}
