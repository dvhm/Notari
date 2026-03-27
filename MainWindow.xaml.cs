using System.Windows;
using PhonKit;

namespace Notari
{
    public partial class MainWindow : Window
    {
        private double _zoom      = 1.0;
        private string _filePath  = string.Empty;
        private bool   _isDirty   = false;
        private int    _wordCount = 0;
        private int    _lineCount = 0;

        private AppSettings _settings = new();
        private System.Windows.Threading.DispatcherTimer? _autoSaveTimer;
        private readonly PhoneticDatabase _db;
        private CancellationTokenSource _lookupCts   = new();
        private CancellationTokenSource _syllableCts = new();
        private EditorAdorner _adorner = null!;

        public MainWindow()
        {
            _settings = AppSettings.Load();
            _db = new PhoneticDatabase();
            InitializeComponent();
            Loaded += (_, _) =>
            {
                InitAdorner();
                ApplySettings(_settings, save: false);
                if (true) // if (!_settings.HasShownStartMessage)
                {
                    _settings.HasShownStartMessage = true;
                    _settings.Save();
                    Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                        () => new Dialogs.StarMessageDialog(this).ShowDialog());
                }
            };
        }

        protected override async void OnClosed(EventArgs e)
        {
            _lookupCts.Cancel();
            _lookupCts.Dispose();
            _syllableCts.Cancel();
            _syllableCts.Dispose();
            await _db.DisposeAsync();
            _autoSaveTimer?.Stop();
            base.OnClosed(e);
        }
    }
}
