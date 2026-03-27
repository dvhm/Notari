using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Notari;

public partial class MainWindow
{
    private void OnSidebarToggled(object sender, RoutedEventArgs e)
    {
        bool collapsed = SidebarToggle.IsChecked == true;
        SidebarColumn.Width = collapsed ? new GridLength(0) : new GridLength(220);
        Sidebar.Visibility  = collapsed ? Visibility.Collapsed : Visibility.Visible;
    }
}
