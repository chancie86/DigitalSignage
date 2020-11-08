using System;
using System.Windows;
using System.Windows.Controls;
using Display.ViewModels;
using WpfUtils;

namespace Display.Views
{
    /// <summary>
    /// Interaction logic for HtmlView.xaml
    /// </summary>
    [DataTemplated(typeof(ChromeHtmlViewModel))]
    public partial class ChromeHtmlView : IDisposable
    {
        private bool _isDisposed;

        public ChromeHtmlView()
        {
            InitializeComponent();

            Unloaded += (sender, args) => Dispose();
        }

        private void FrameworkElement_OnLoaded(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.Cursor.Hide();
        }

        #region Implements IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;

            if (disposing)
            {
                try
                {
                    Browser.Dispose();
                }
                catch (Exception ex)
                {
                    Log.TraceErr("Error whilst disposing WebBrowser. {0}", ex.ToString());
                }
            }

            _isDisposed = true;
        }
        #endregion
    }
}
