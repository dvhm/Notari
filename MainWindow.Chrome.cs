using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Notari
{
    public partial class MainWindow : Window
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUNDSMALL              = 3;
        private const int DWMWCP_ROUND                   = 2;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwnd = new WindowInteropHelper(this).Handle;
            int pref = DWMWCP_ROUND;
            DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref pref, sizeof(int));
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
