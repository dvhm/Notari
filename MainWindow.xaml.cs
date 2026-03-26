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

        private readonly PhoneticDatabase _db;
        private CancellationTokenSource _lookupCts = new();

        public MainWindow()
        {
            _db = new PhoneticDatabase();
            InitializeComponent();
        }

        protected override async void OnClosed(EventArgs e)
        {
            _lookupCts.Cancel();
            _lookupCts.Dispose();
            await _db.DisposeAsync();
            base.OnClosed(e);
        }
    }
}
