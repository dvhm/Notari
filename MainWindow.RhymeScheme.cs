using System.Windows;

namespace Notari;

public partial class MainWindow
{
    private void OnRhymeSchemeToggled(object sender, RoutedEventArgs e)
    {
        if (RhymeSchemeToggle.IsChecked != true)
        {
            _adornerService.SetRhymeLabels([]);
            return;
        }
        _ = _vm.UpdateRhymeSchemeAsync();
    }
}


