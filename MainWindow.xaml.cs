using System.Windows;
using PhonKit;
using Notari.Services;
using Notari.ViewModels;

namespace Notari
{
    public partial class MainWindow : Window
    {
        private double _zoom     = 1.0;
        private string _filePath = string.Empty;
        private bool   _isDirty  = false;
        private int    _wordCount = 0;
        private int    _lineCount = 0;

        private AppSettings _settings;
        private System.Windows.Threading.DispatcherTimer? _autoSaveTimer;
        private EventHandler? _statsDebounceTickHandler;
        private EventHandler? _autoSaveTickHandler;
        private readonly System.Windows.Threading.DispatcherTimer _hoverTimer;
        private System.Windows.Point _hoverPoint;

        private readonly ILookupService       _lookupService;
        private readonly IAdornerService      _adornerService;
        private readonly ITextAnalysisService _textAnalysis;
        private IDocumentService     _docService = null!; // set in OnLoaded
        private MainWindowViewModel  _vm         = null!; // set in OnLoaded

        public MainWindow(
            AppSettings          settings,
            ITextAnalysisService textAnalysis,
            ILookupService       lookupService,
            IAdornerService      adornerService)
        {
            _settings       = settings;
            _textAnalysis   = textAnalysis;
            _lookupService  = lookupService;
            _adornerService = adornerService;
            _hoverTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _hoverTimer.Tick += OnHoverTimerTick;
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            _adornerService.Initialize(Editor);
            _docService = new DocumentService(Editor, _textAnalysis);
            _vm         = new MainWindowViewModel(_lookupService, _docService, _textAnalysis, _adornerService);

            _vm.LookupCleared       += OnLookupCleared;
            _vm.LookupCompleted     += OnLookupCompleted;
            _vm.SyllableCountsReady += OnSyllableCountsReady;
            _vm.RhymeSchemeReady    += OnRhymeSchemeReady;
            _vm.HoverReady          += OnHoverReady;
            _vm.HoverCleared        += OnHoverCleared;
            _vm.PropertyChanged     += OnViewModelPropertyChanged;

            InitFindReplace();
            _statsDebounceTickHandler = (_, _) => { _statsDebounce.Stop(); _vm.UpdateDocumentStats(); };
            _statsDebounce.Tick += _statsDebounceTickHandler;
            ApplySettings(_settings, save: false);

            if (!_settings.HasShownStartMessage)
            {
                _settings.HasShownStartMessage = true;
                _ = _settings.SaveAsync();
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                    () => new Dialogs.StarMessageDialog(this).ShowDialog());
            }
        }

        protected override async void OnClosed(EventArgs e)
        {
            _hoverTimer.Stop();
            _hoverTimer.Tick -= OnHoverTimerTick;
            _statsDebounce.Stop();
            if (_statsDebounceTickHandler is not null)
                _statsDebounce.Tick -= _statsDebounceTickHandler;
            if (_autoSaveTimer is not null && _autoSaveTickHandler is not null)
                _autoSaveTimer.Tick -= _autoSaveTickHandler;
            _autoSaveTimer?.Stop();
            await _vm.DisposeAsync();
            base.OnClosed(e);
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(MainWindowViewModel.WordCount) or nameof(MainWindowViewModel.LineCount))
            {
                _wordCount = _vm.WordCount;
                _lineCount = _vm.LineCount;
                UpdateTitle();
            }
            else if (e.PropertyName == nameof(MainWindowViewModel.IsLookupBusy))
            {
                LookupSpinner.Visibility = _vm.IsLookupBusy ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }
}
