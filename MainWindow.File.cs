using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using IOPath = System.IO.Path;

namespace Notari
{
    public partial class MainWindow : Window
    {
        private void SetDirty(bool dirty)
        {
            _isDirty = dirty;
            string name = string.IsNullOrEmpty(_filePath) ? "Untitled" : IOPath.GetFileName(_filePath);
            TitleLabel.Text = dirty ? $"{name}*" : name;
        }

        private bool ConfirmDiscard()
        {
            if (!_isDirty) return true;
            var dialog = new Dialogs.UnsavedChangesDialog(this);
            dialog.ShowDialog();
            return dialog.Discard;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!ConfirmDiscard())
                e.Cancel = true;
            base.OnClosing(e);
        }

        private void OnNew(object sender, ExecutedRoutedEventArgs e)
        {
            if (!ConfirmDiscard()) return;

            Editor.Document.Blocks.Clear();
            _filePath = string.Empty;
            SetDirty(false);
        }

        private void OnOpenFile(object sender, ExecutedRoutedEventArgs e)
        {
            if (!ConfirmDiscard()) return;

            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title  = "Open file",
                Filter = "Supported files|*.rtf;*.txt|Rich Text Format|*.rtf|Plain Text|*.txt",
            };

            if (dialog.ShowDialog() != true)
                return;

            _filePath = dialog.FileName;
            LoadFile(_filePath);
            SetDirty(false);
        }

        private void OnSave(object sender, ExecutedRoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_filePath))
            {
                OnSaveAs(sender, e);
                return;
            }
            SaveFile(_filePath);
        }

        private void OnSaveAs(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title      = "Save file",
                Filter     = "Rich Text Format|*.rtf|Plain Text|*.txt",
                DefaultExt = ".rtf",
                FileName   = IOPath.GetFileName(_filePath),
            };

            if (dialog.ShowDialog() != true)
                return;

            _filePath = dialog.FileName;
            SaveFile(_filePath);
            SetDirty(false);
        }

        private void OnExit(object sender, ExecutedRoutedEventArgs e) => Application.Current.Shutdown();

        private void LoadFile(string path)
        {
            string ext   = IOPath.GetExtension(path).ToLowerInvariant();
            var    range = new TextRange(Editor.Document.ContentStart, Editor.Document.ContentEnd);

            using var stream = File.OpenRead(path);
            range.Load(stream, ext == ".rtf" ? DataFormats.Rtf : DataFormats.Text);
        }

        private void SaveFile(string path)
        {
            string ext   = IOPath.GetExtension(path).ToLowerInvariant();
            var    range = new TextRange(Editor.Document.ContentStart, Editor.Document.ContentEnd);

            using var stream = File.Open(path, FileMode.Create);
            range.Save(stream, ext == ".rtf" ? DataFormats.Rtf : DataFormats.Text);

            SetDirty(false);
        }
    }
}
