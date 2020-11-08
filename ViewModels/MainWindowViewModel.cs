using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Threading;
using WpfUtils.ViewModels;

namespace Display.ViewModels
{
    public class MainWindowViewModel
        : WindowBaseViewModel, IDisposable
    {
        private readonly Random _random;
        private readonly object _displayLock = new object();

        private bool _stopTimeBackgroundWorker;
        private bool _stopDisplayBackgroundWorker;
        private bool _stopConfigBackgroundWorker;

        private Configuration _config;

        public MainWindowViewModel()
        {
            Initializing = true;
#if !DEBUG
            Fullscreen = true;
#endif
            _random = new Random();
            DisplaysToShow = new ObservableCollection<DisplayBaseViewModel>();

            Initialize();
        }

        public bool Initializing
        {
            get { return PropertyBag.GetAuto<bool>(); }
            set { PropertyBag.SetAuto(value); }
        }

        public bool ConfigLoaded
        {
            get { return PropertyBag.GetAuto<bool>(); }
            set { PropertyBag.SetAuto(value); }
        }

        public string Time
        {
            get { return PropertyBag.GetAuto<string>(); }
            set { PropertyBag.SetAuto(value); }
        }

        public DisplayBaseViewModel Content
        {
            get { return PropertyBag.GetAuto<DisplayBaseViewModel>(); }
            set { PropertyBag.SetAuto(value); }
        }

        public ObservableCollection<DisplayBaseViewModel> DisplaysToShow { get; private set; }

        private void Initialize()
        {
            Log.TraceMsg("MainViewModel: Initializing");

            // The clock
            var timeBackgroundWorker = new BackgroundWorker();
            timeBackgroundWorker.DoWork += UpdateTime;
            timeBackgroundWorker.RunWorkerAsync();

            RefreshConfiguration();

            // Periodically reload the config
            var configBackgroundWorker = new BackgroundWorker();
            configBackgroundWorker.DoWork += RefreshConfiguration;
            configBackgroundWorker.RunWorkerCompleted += (sender, args) => Log.TraceMsg("configBackgroundWorker thread exited");
            configBackgroundWorker.RunWorkerAsync();

            // The content to display
            var displayBackgroundWorker = new BackgroundWorker();
            displayBackgroundWorker.DoWork += ShowNextDisplay;
            displayBackgroundWorker.RunWorkerCompleted += (sender, args) => Log.TraceMsg("displayBackgroundWorker thread exited");
            displayBackgroundWorker.RunWorkerAsync();

            Initializing = false;
        }

        private void ShowNextDisplay(object sender, DoWorkEventArgs e)
        {
            while (!_stopDisplayBackgroundWorker)
            {
                try
                {
                    lock (_displayLock)
                    {
                        if (DisplaysToShow.Count == 0)
                        {
                            // Eek, nothing to display? Keep polling regularly in case the config file is updated

                            Log.TraceMsg("MainWindowViewModel.ShowNextDisplay: No displays loaded yet. Waiting a short period...");

                            Thread.Sleep(30 * 1000);
                            continue;
                        }

                        NextDisplay();
                    }

                    Thread.Sleep(Content == null ? 1000*10 : Content.DisplayIntervalInSeconds * 1000);
                }
                catch (Exception ex)
                {
                    Log.TraceErr("ShowNextDisplay: Error. Retrying in 30 seconds. {0}", ex.ToString());
                    Thread.Sleep(30 * 1000);
                }
            }
        }

        #region Configuration
        // Hard code the config for now. In the future make it load an xml doc.
        private Configuration LoadConfiguration()
        {
            const string configFileName = "Config.xml";

            // Try to load from profile directory first
            var configFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Display");
            var configFilePath = Path.Combine(configFolderPath, configFileName);

            if (File.Exists(configFilePath))
            {
                Log.TraceMsg("Loading config from profile path");
            }
            else
            {
                Log.TraceMsg("Loading config from exe path");
                configFolderPath = Directory.GetParent(Assembly.GetExecutingAssembly().Location).ToString();
                configFilePath = Path.Combine(configFolderPath, configFileName);
            }

            _config = Configuration.Load(configFilePath, _config);

            return _config;
        }

        private void RefreshConfiguration()
        {
            Log.TraceEntry();

            try
            {
                var config = LoadConfiguration();

                lock (_displayLock)
                {
                    DisplaysToShow.Clear();

                    foreach (var display in config.AllDisplays)
                    {
                        DisplaysToShow.Add(display);
                    }
                }

                ConfigLoaded = true;
            }
            catch (Exception ex)
            {
                Log.TraceErr("MainViewModel.RefreshConfiguration: Failed to load configuration file. {0}", ex.ToString());

                ConfigLoaded = false;
            }

            Log.TraceMsg("MainViewModel.RefreshConfiguration: Exit");
        }

        private void RefreshConfiguration(object sender, DoWorkEventArgs e)
        {
            while (!_stopConfigBackgroundWorker)
            {
                try
                {
                    if (DisplaysToShow.Count == 0)
                        Thread.Sleep(60*1000);
                    else
                        Thread.Sleep(3*60*60*1000); // 3 hours

                    RefreshConfiguration();
                }
                catch (Exception ex)
                {
                    Log.TraceErr("MainWindowViewModel.RefreshConfiguration: Error: ", ex.ToString());
                }
            }
        }
        #endregion

        #region Local Time
        private void UpdateTime(object sender, DoWorkEventArgs e)
        {
            var tickTock = true;
            const string tickFormat = "HH:mm";
            const string tockFormat = "HH mm";

            while (!_stopTimeBackgroundWorker)
            {
                switch (tickTock)
                {
                    case true:
                        Time = DateTime.Now.ToString(tickFormat);
                        break;
                    default:
                        Time = DateTime.Now.ToString(tockFormat);
                        break;
                }

                Thread.Sleep(500);
                tickTock = !tickTock;
            }
        }
        #endregion

        public void Dispose()
        {
            _stopTimeBackgroundWorker = true;
            _stopDisplayBackgroundWorker = true;
            _stopConfigBackgroundWorker = true;
        }

        public void NextDisplay()
        {
            if (DisplaysToShow.Count == 0)
            {
                return;
            }

            var displayIndex = _random.Next(0, DisplaysToShow.Count);
            
            var content = DisplaysToShow[displayIndex];

            if (!content.Initialized)
            {
                // Try to initialize the display
                content.RaiseRefresh();
            }

            if (content == null
                || !content.IsVisible)      // Display has reported that it shouldn't be displayed
                return;

            Log.TraceMsg("Showing next display '{0}'", content.GetType());

            if (Content != null)
                Content.RaiseDisplayHidden();

            Content = content;
            Content.RaiseDisplayShown();
        }
    }
}
