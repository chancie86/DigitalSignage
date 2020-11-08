using System;
using System.Windows.Controls;
using Display.ViewModels;
using WpfUtils;

namespace Display.Views
{
    /// <summary>
    /// Interaction logic for PowerPointDisplayView.xaml
    /// </summary>
    [DataTemplated(typeof(PowerPointDisplayViewModel))]
    public partial class PowerPointDisplayView : UserControl
    {
        public PowerPointDisplayView()
        {
            InitializeComponent();
        }
    }
}
