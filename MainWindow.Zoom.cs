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

            double delta = e.Delta > 0 ? ZoomStep : -ZoomStep;
            _zoom = Math.Clamp(_zoom + delta, ZoomMin, ZoomMax);

            PaperScale.ScaleX = _zoom;
            PaperScale.ScaleY = _zoom;
            ZoomLabel.Text    = $"{(int)Math.Round(_zoom * 100)}%";
        }
    }
}
