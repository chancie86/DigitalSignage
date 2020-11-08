using System.Windows.Controls;
using Display.ViewModels;
using Display.ViewModels.Clocks;
using WpfUtils;

namespace Display.Views
{
    /// <summary>
    /// Interaction logic for ClocksDisplayView.xaml
    /// </summary>
    [DataTemplated(typeof(ClocksDisplayViewModel))]
    public partial class ClocksDisplayView : UserControl
    {
        public ClocksDisplayView()
        {
            InitializeComponent();
        }
    }
}
