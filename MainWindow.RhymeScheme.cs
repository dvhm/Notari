using System.Windows;
using System.Windows.Input;

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

    private void OnToggleRhymeScheme(object sender, ExecutedRoutedEventArgs e) =>
        RhymeSchemeToggle.IsChecked = !RhymeSchemeToggle.IsChecked;
}


