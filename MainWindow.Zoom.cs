using System.Windows;
using System.Windows.Input;

namespace Notari
{
    public partial class MainWindow : Window
    {
        private const double ZoomStep = 0.1;
        private const double ZoomMin  = 0.25;
        private const double ZoomMax  = 4.0;

        private void OnEditorCanvasWheel(object sender, MouseWheelEventArgs e)
        {
            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
                return;

            e.Handled = true;

            double delta   = e.Delta > 0 ? ZoomStep : -ZoomStep;
            double newZoom = Math.Clamp(_zoom + delta, ZoomMin, ZoomMax);
            if (newZoom == _zoom) return;

            _zoom = newZoom;
            PaperScale.ScaleX = _zoom;
            PaperScale.ScaleY = _zoom;
            ZoomLabel.Text    = $"{(int)Math.Round(_zoom * 100)}%";
        }
    }
}
