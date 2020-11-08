using System.Windows.Controls;
using Display.ViewModels;
using WpfUtils;

namespace Display.Views
{
    /// <summary>
    /// Interaction logic for MoneypennysView.xaml
    /// </summary>
    [DataTemplated(typeof(MoneypennysViewModel))]
    public partial class MoneypennysView : UserControl
    {
        public MoneypennysView()
        {
            InitializeComponent();
        }
    }
}
