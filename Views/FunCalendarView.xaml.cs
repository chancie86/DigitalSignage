using System.Windows.Controls;
using Display.ViewModels;
using WpfUtils;

namespace Display.Views
{
    /// <summary>
    /// Interaction logic for FunCalendarViewModel.xaml
    /// </summary>
    [DataTemplated(typeof(FunCalendarViewModel))]
    public partial class FunCalendarView : UserControl
    {
        public FunCalendarView()
        {
            InitializeComponent();
        }
    }
}
