using System.Windows.Controls;
using Display.ViewModels;
using Display.ViewModels.Clocks;
using WpfUtils;

namespace Display.Views
{
    /// <summary>
    /// Interaction logic for ClockView.xaml
    /// </summary>
    [DataTemplated(typeof(ClockViewModel))]
    public partial class ClockView : UserControl
    {
        public ClockView()
        {
            InitializeComponent();
        }
    }
}
