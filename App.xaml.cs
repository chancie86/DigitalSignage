using System.Windows;
using Display.ViewModels;
using WpfUtils;

namespace Display
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void OnStartup(object sender, StartupEventArgs e)
        {
            AutoDataTemplateResourceDictionary.LoadAutoDataTemplates();

            var mainWindowViewModel = new MainWindowViewModel();
            Utilities.Show(mainWindowViewModel, true);
        }
    }
}
