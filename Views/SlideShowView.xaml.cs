using System;
using System.Windows;
using System.Windows.Controls;
using Display.ViewModels;
using WpfUtils;

namespace Display.Views
{
    /// <summary>
    /// Interaction logic for SlideShowView.xaml
    /// </summary>
    [DataTemplated(typeof(SlideShowViewModel))]
    public partial class SlideShowView : UserControl
    {
        public SlideShowView()
        {
            InitializeComponent();
        }

        private void OnMediaEnded(object sender, RoutedEventArgs e)
        {
            var m = sender as MediaElement;
            if (m == null)
                return;

            m.Position = TimeSpan.FromSeconds(1);
        }

        private void OnMediaOpened(object sender, RoutedEventArgs e)
        {
            var m = sender as MediaElement;
            if (m == null)
                return;

            m.Width = m.NaturalVideoWidth;
            m.Height = m.NaturalVideoHeight;
        }
    }
}
