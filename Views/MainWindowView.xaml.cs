using System.Windows.Controls;
using System.Windows.Input;
using Display.ViewModels;
using WpfUtils;

namespace Display.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [DataTemplated(typeof(MainWindowViewModel))]
    public partial class MainWindowView : UserControl
    {
        public MainWindowView()
        {
            InitializeComponent();
        }

        private void MainWindowViewOnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var viewModel = DataContext as MainWindowViewModel;

            if (viewModel == null)
                return;

            viewModel.NextDisplay();
        }
    }
}
