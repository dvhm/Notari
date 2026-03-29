using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace Notari;

public partial class MainWindow
{
    private void OnTakeScreenshot(object sender, ExecutedRoutedEventArgs e)
    {
        double scale = _settings.ScreenshotScale;
        int pw = (int)(ActualWidth  * scale);
        int ph = (int)(ActualHeight * scale);

        var rtb = new RenderTargetBitmap(pw, ph, 96.0 * scale, 96.0 * scale, PixelFormats.Pbgra32);
        rtb.Render(this);

        var dlg = new SaveFileDialog
        {
            Title            = "Save screenshot",
            Filter           = "PNG image|*.png",
            FileName         = $"Notari-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        };

        if (dlg.ShowDialog(this) != true) return;

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(rtb));
        using var stream = File.OpenWrite(dlg.FileName);
        encoder.Save(stream);
    }
}
