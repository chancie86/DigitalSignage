using System.Windows.Controls;
using Display.ViewModels.Twitter;
using WpfUtils;

namespace Display.Views
{
    /// <summary>
    /// Interaction logic for TwitterView.xaml
    /// </summary>
    [DataTemplated(typeof(TwitterViewModel))]
    public partial class TwitterView : UserControl
    {
        public TwitterView()
        {
            InitializeComponent();
        }
    }
}
