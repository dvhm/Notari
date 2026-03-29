using System.Windows;
using Notari.Services;
using PhonKit;

namespace Notari
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var settings      = AppSettings.Load();
            var textAnalysis  = new TextAnalysisService();
            var lookupService = new LookupService(new PhoneticDatabase());
            var adornerService = new AdornerService();

            var window = new MainWindow(settings, textAnalysis, lookupService, adornerService);
            window.Show();
        }
    }
}
