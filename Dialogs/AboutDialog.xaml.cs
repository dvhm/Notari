using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace Notari.Dialogs;

public partial class AboutDialog : Window
{   
    public AboutDialog(Window owner)
    {
        Owner = owner;
        InitializeComponent();

        var versionFile = System.IO.Path.Combine(AppContext.BaseDirectory, "version.txt");
        var version = System.IO.File.Exists(versionFile)
            ? System.IO.File.ReadAllText(versionFile).Trim()
            : "?";
        VersionText.Text = $"Notari v{version}";
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        NativeWindowHelper.ApplyRoundedCorners(new WindowInteropHelper(this).Handle);
    }

    private void OpenLink(object sender, MouseButtonEventArgs e)
    {
        Process.Start(new ProcessStartInfo("https://github.com/dvhm/notari")
        {
            UseShellExecute = true
        });
    }
    private void OnOk(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
