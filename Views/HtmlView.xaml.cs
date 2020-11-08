using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Navigation;
using Display.ViewModels;
using WpfUtils;

namespace Display.Controls
{
    /// <summary>
    /// Interaction logic for HtmlView.xaml
    /// </summary>
    [DataTemplated(typeof(HtmlViewModel))]
    public partial class HtmlView : UserControl, IDisposable
    {
        private bool _isDisposed;

        public HtmlView()
        {
            InitializeComponent();

            Unloaded += (sender, args) => Dispose();
            Browser.Navigated += new NavigatedEventHandler(OnNavigated);
        }

        private HtmlViewModel ViewModel
        {
            get
            {
                return DataContext as HtmlViewModel;
            }
        }

        private void HtmlView_OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (ViewModel == null)
                return;

            Browser.Navigate(ViewModel.Address);
        }

        private void Browser_OnLoaded(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.Cursor.Hide();
        }

        private void OnNavigated(object sender, NavigationEventArgs e)
        {
            SuppressScriptingErrors();
        }

        private void SuppressScriptingErrors()
        {
            // get an IWebBrowser2 from the document
            IOleServiceProvider sp = Browser.Document as IOleServiceProvider;
            if (sp != null)
            {
                Guid IID_IWebBrowserApp = new Guid("0002DF05-0000-0000-C000-000000000046");
                Guid IID_IWebBrowser2 = new Guid("D30C1661-CDAF-11d0-8A3E-00C04FC9E26E");

                object webBrowser;
                sp.QueryService(ref IID_IWebBrowserApp, ref IID_IWebBrowser2, out webBrowser);
                if (webBrowser != null)
                {
                    webBrowser.GetType().InvokeMember("Silent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.PutDispProperty, null, webBrowser, new object[] { true });
                }
            }
        }

        [ComImport, Guid("6D5140C1-7436-11CE-8034-00AA006009FA"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IOleServiceProvider
        {
            [PreserveSig]
            int QueryService([In] ref Guid guidService, [In] ref Guid riid, [MarshalAs(UnmanagedType.IDispatch)] out object ppvObject);
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
                // There's a memory leak issue in the WebBrowser control. This code attempts to force it to clean up its resources.
                // Apparently, it's to do with the fact that WebBrowser.Dispose doesn't call IKeyboardInputSite.Unregister(), so we
                // call it manually here. Not sure if this actually works as I haven't been able to repro.
                Browser.Dispose();
                BindingOperations.ClearAllBindings(Browser);

                try
                {
                    var window = GetWindowElement(Application.Current.MainWindow);

                    if (window == null)
                        return;

                    var swhValue = GetValueFromField("_swh", window);

                    if (swhValue == null)
                        return;

                    var sourceWindowValue = GetValueFromField("_sourceWindow", swhValue);

                    if (sourceWindowValue == null)
                        return;

                    var keyboardInputSinkChildrenValue = GetValueFromField("_keyboardInputSinkChildren", sourceWindowValue);

                    var inputSites = keyboardInputSinkChildrenValue as IEnumerable<IKeyboardInputSite>;

                    if (inputSites == null)
                        return;

                    var currentSite = inputSites.FirstOrDefault(s => ReferenceEquals(s.Sink, Browser));

                    if (currentSite != null)
                    {
                        Log.TraceMsg("Found rogue keyboard input sinks. Unregistering.");
                        currentSite.Unregister();
                    }
                }
                catch (Exception ex)
                {
                    Log.TraceErr("Error whilst disposing WebBrowser. {0}", ex.ToString());
                }
            }

            _isDisposed = true;
        }

        private static Window GetWindowElement(DependencyObject element)
        {
            while (element != null && !(element is Window))
            {
                element = VisualTreeHelper.GetParent(element);
            }

            return element as Window;
        }

        private static object GetValueFromField(string field, object objectInstance, BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic)
        {
            var sourceType = objectInstance.GetType();
            var swhFieldInfo = sourceType.GetField(field, flags);

            return swhFieldInfo == null ? null : swhFieldInfo.GetValue(objectInstance);
        }
        #endregion
    }
}
