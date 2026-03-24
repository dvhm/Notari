using System.Windows;
using PhonKit.Synset;

namespace Notari
{
    public partial class MainWindow : Window
    {
        private double _zoom     = 1.0;
        private string _filePath = string.Empty;
        private bool   _isDirty  = false;

        private readonly SynsetEngine _synsetEngine;

        public MainWindow()
        {
            _synsetEngine = new SynsetEngine();
            InitializeComponent();
        }
    }
}
