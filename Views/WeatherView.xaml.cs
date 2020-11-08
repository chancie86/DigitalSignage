using System.Windows.Controls;
using Display.ViewModels.Weather;
using WpfUtils;

namespace Display.Views
{
    /// <summary>
    /// Interaction logic for WeatherView.xaml
    /// </summary>
    [DataTemplated(typeof(WeatherDisplayViewModel))]
    public partial class WeatherView : UserControl
    {
        public WeatherView()
        {
            InitializeComponent();
        }
    }
}
