using System;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using WpfUtils;

namespace Display.ViewModels
{
    public delegate void DisplayShownEventHandler();
    public delegate void RefreshEventHandler();

    public class DisplayBaseViewModel
        : BaseViewModel, IDisposable
    {
        public event DisplayShownEventHandler DisplayShown;
        public event DisplayShownEventHandler DisplayHidden;
        public event RefreshEventHandler Refresh;

        private static string _title;

        private bool _stopRefresh;
        private bool _isDisposed;
        
        protected DisplayBaseViewModel()
        {
            if (string.IsNullOrEmpty(_title))
            {
                var domain = Environment.UserDomainName;
                var hostName = Dns.GetHostName();
                var ip = Dns.GetHostAddresses(hostName).FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);    // IPv4

                _title = domain + "\\" + hostName + " (" + ip + ")";
            }

            Title = _title;
            DisplayIntervalInSeconds = 180;
            IsVisible = true;

            var displayBackgroundWorker = new BackgroundWorker();
            displayBackgroundWorker.DoWork += OnRefresh;
            displayBackgroundWorker.RunWorkerAsync();
        }

        internal bool Initialized { get; private set; }

        public int DisplayIntervalInSeconds { get; set; }

        internal void RaiseDisplayShown()
        {
            if (DisplayShown != null)
            {
                DisplayShown();
            }
        }

        internal void RaiseDisplayHidden()
        {
            if (DisplayHidden != null)
            {
                DisplayHidden();
            }
        }

        internal void RaiseRefresh()
        {
            if (Refresh != null)
            {
                try
                {
                    Refresh();
                    Initialized = true;
                }
                catch (Exception ex)
                {
                    Log.TraceErr("Refresh failed for '{0}'. Exception: {1}", GetType(), ex.ToString());
                }
            }  
        }

        public string Title
        {
            get { return PropertyBag.GetAuto<string>(); }
            set { PropertyBag.SetAuto(value); }
        }

        public bool IsVisible
        {
            get { return PropertyBag.GetAuto<bool>(); }
            set { PropertyBag.SetAuto(value); }
        }

        #region Background data polling
        protected virtual int RefreshIntervalInMinutes { get { return 0; } }

        private void OnRefresh(object sender, DoWorkEventArgs e)
        {
            if (RefreshIntervalInMinutes <= 0)
                return;

            var lastRefresh = DateTime.UtcNow;

            while (!_stopRefresh)
            {
                var now = DateTime.UtcNow;

                if (lastRefresh.AddMinutes(RefreshIntervalInMinutes) < now)
                {
                    lastRefresh = now;
                    RaiseRefresh();
                }

                System.Threading.Thread.Sleep(5000);
            }
        }
        #endregion

        #region Implements IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
            Log.TraceErr("DisplayBaseViewModel.Dispose: {0}", GetType());
                    _stopRefresh = true;
                }

                _isDisposed = true;
            }
        }
        #endregion
    }
}
