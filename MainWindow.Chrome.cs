using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Notari
{
    public partial class MainWindow : Window
    {
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            NativeWindowHelper.ApplyRoundedCorners(new WindowInteropHelper(this).Handle);
        }

        private void OnCaptionMinimize(object sender, RoutedEventArgs e) =>
            WindowState = WindowState.Minimized;

        private void OnCaptionMaximize(object sender, RoutedEventArgs e) =>
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            bool maximized = WindowState == WindowState.Maximized;
            IconMaximize.Visibility = maximized ? Visibility.Collapsed : Visibility.Visible;
            IconRestore.Visibility  = maximized ? Visibility.Visible   : Visibility.Collapsed;
        }
    }
}
