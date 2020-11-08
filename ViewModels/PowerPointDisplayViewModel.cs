using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using PPT = Microsoft.Office.Interop.PowerPoint;
using Microsoft.Win32;

namespace Display.ViewModels
{
    public class PowerPointDisplayViewModel
        : DisplayBaseViewModel
    {
        private const string PowerPointFileName = "powerpnt.exe";
        
        private const string PowerPointRegKeyPathFormat = @"SOFTWARE\Microsoft\Office\{0}\PowerPoint\InstallRoot";
        private const string PowerPointRegValueName = "Path";

        private static readonly string PowerPoint2010ViewerRegKeyPath = string.Format(PowerPointRegKeyPathFormat, "14.0");
        private static readonly string PowerPoint2013ViewerRegKeyPath = string.Format(PowerPointRegKeyPathFormat, "15.0");
        private static readonly string PowerPoint2016ViewerRegKeyPath = string.Format(PowerPointRegKeyPathFormat, "16.0");

        private static readonly string[] PossiblePowerPointRegKeyPaths = {
                                                                             // Note the ordering here.
                                                                             PowerPoint2016ViewerRegKeyPath,
                                                                             PowerPoint2013ViewerRegKeyPath,
                                                                             PowerPoint2010ViewerRegKeyPath
                                                                         };

        private string _exePath;

        private readonly BackgroundWorker _slideshowController;

        private PPT.Slides _slides;
        private PPT.Application _pptApplication;
        private int _currentSlideIndex;

        public PowerPointDisplayViewModel(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Could not load PowerPoint", filePath);
            }

            Title = "PowerPoint - " + Path.GetFileName(filePath);
            Arguments = @"/s """ + filePath + @"""";
            FilePath = filePath;

            DisplayShown += OnDisplayShown;
            Refresh += Cleanup;
            _slideshowController = new BackgroundWorker();
            _slideshowController.DoWork += FlickThroughSides;
        }

        protected override int RefreshIntervalInMinutes
        {
            get { return 60 * 12; }
        }

        private void FlickThroughSides(object sender, DoWorkEventArgs e)
        {
            Log.TraceEntry();

            var secondsOnEachSlide = DisplayIntervalInSeconds / _slides.Count;

            while (_currentSlideIndex < _slides.Count)
            {
                System.Threading.Thread.Sleep(secondsOnEachSlide * 1000);
                SetSlide(++_currentSlideIndex);
            }

            Log.TraceExit();
        }

        #region Lifecycle hooks
        private void OnDisplayShown()
        {
            _slides = null;
            _pptApplication = null;

            // Go to the first slide and then flick through the presentation
            const int timeout = 20;
            var numTimesRetried = 0;
            while (_slides == null)
            {
                if (numTimesRetried++ > timeout)
                    return;

                try
                {
                    // Get Running PowerPoint Application object
                    _pptApplication = Marshal.GetActiveObject("PowerPoint.Application") as PPT.Application;
                }
                catch (Exception ex)
                {
                    Log.TraceErr("PowerPointDisplayViewModel.OnDisplayShown: Couldn't find PowerPoint. {0}", ex.Message);
                    _pptApplication = null;
                }

                if (_pptApplication == null)
                {
                    Log.TraceErr("PowerPointDisplayViewModel.OnDisplayShown: Waiting for PowerPoint to load...");
                    System.Threading.Thread.Sleep(2000);
                }
                else
                {
                    break;
                }
            }

            if (_pptApplication == null)
            {
                Log.TraceErr("PowerPointDisplayViewModel.OnDisplayShown: Unable to obtain a running instance of PowerPoint, giving up");
                return;
            }

            // Get Presentation Object
            var presentation = _pptApplication.ActivePresentation;

            // Get Slide collection object
            _slides = presentation.Slides;

            // Go to the first slide
            SetSlide(1);

            _slideshowController.RunWorkerAsync();
        }

        private void Cleanup()
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.InternetCache), "Content.MSO");
            var files = Directory.GetFiles(path, "*.pptx", SearchOption.AllDirectories);

            Log.TraceMsg("PowerPointDisplayViewModel.OnDisplayHidden: Cleaning up temporary pptx files");

            foreach (var file in files)
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    Log.TraceErr("PowerPointDisplayViewModel.OnDisplayHidden: Unable to delete file {0}. {1}", file, ex.ToString());
                }
            }
        }
        #endregion

        public string ExePath
        {
            get
            {
                if (!string.IsNullOrEmpty(_exePath))
                    return _exePath;

                using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
                {
                    foreach (var pptKegKeyPath in PossiblePowerPointRegKeyPaths)
                    {
                        using (var pptKey = baseKey.OpenSubKey(pptKegKeyPath))
                        {
                            if (pptKey == null)
                                continue;

                            var path = pptKey.GetValue(PowerPointRegValueName) as string;

                            if (path != null)
                                _exePath = Path.Combine(path, PowerPointFileName);
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(_exePath))
                    throw new Exception("PowerPoint Viewer path could not be found. It may not be installed.");

                return _exePath;
            }
        }

        public string FilePath { get; private set; }

        public string Arguments { get; private set; }
        
        private void SetSlide(int slideNumber)
        {
            try
            {
                // Call Select method to select slide in normal view
                _slides[slideNumber].Select();

                Log.TraceMsg("PowerPointDisplayViewModel.SetSlide: Switched to slide {0} in normal view.", slideNumber);
            }
            catch(Exception)
            {
                // Change page in reading view
                _pptApplication.SlideShowWindows[1].View.GotoSlide(slideNumber);

                Log.TraceMsg("PowerPointDisplayViewModel.SetSlide: Switched to slide {0} in reading view.", slideNumber);
            }

            _currentSlideIndex = slideNumber;
        }
    }
}
