using System.Windows.Controls;
using Display.ViewModels;
using WpfUtils;

namespace Display.Views
{
    /// <summary>
    /// Interaction logic for BuildServerView.xaml
    /// </summary>
    [DataTemplated(typeof(BuildServerViewModel))]
    public partial class BuildServerView : UserControl
    {
        public BuildServerView()
        {
            InitializeComponent();
        }
    }
}
