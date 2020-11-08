using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Display.ViewModels
{
    public class SlideShowViewModel
        : DisplayBaseViewModel
    {
        private static readonly string[] _fileTypes = {".jpg",".png","*.gif"};

        private readonly Random _random;
        protected readonly object FilePathsLock = new object();

        public SlideShowViewModel(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
                throw new ArgumentNullException(imagePath);
            
            Path = imagePath;
            DisplayShown += DisplayShownHandler;
            IsStaticImage = true;

            Log.TraceMsg("SlideShowViewModel: Creating slideshow at '{0}'", imagePath);

            _random = new Random();

            Refresh += LoadImageFilePaths;
        }

        public string Path { get; private set; }

        public bool FilesAvailable
        {
            get { return PropertyBag.GetAuto<bool>(); }
            set { PropertyBag.SetAuto(value); }
        }

        public bool IsStaticImage
        {
            get { return PropertyBag.GetAuto<bool>(); }
            set { PropertyBag.SetAuto(value); }
        }

        public string ImageSource
        {
            get { return PropertyBag.GetAuto<string>(); }
            set { PropertyBag.SetAuto(value); }
        }

        public string PathToDisplay
        {
            get { return PropertyBag.GetAuto<string>(); }
            set { PropertyBag.SetAuto(value); }
        }

        protected IList<string> FilePaths { get; set; }

        protected virtual void LoadImageFilePaths()
        {
            lock (FilePathsLock)
            {
                Log.TraceEntry();

                try
                {
                    var allFiles = Directory.EnumerateFiles(Path, "*", SearchOption.AllDirectories).ToArray();
                    FilePaths = (from t in _fileTypes from f in allFiles where f.EndsWith(t, StringComparison.OrdinalIgnoreCase) select f).ToList();

                    FilesAvailable = FilePaths.Count != 0;
                }
                catch (Exception ex)
                {
                    Log.TraceMsg("SlideShowViewModel.LoadImageFilePaths: Couldn't load image files. {0}", ex.ToString());

                    FilePaths = null;
                    FilesAvailable = false;
                    PathToDisplay = "Couldn't load image files. " + ex.Message;
                }

                Log.TraceExit();
            }
        }

        protected virtual void DisplayShownHandler()
        {
            if (FilePaths == null)
                LoadImageFilePaths();

            lock (FilePathsLock)
            {
                if (FilePaths == null
                    || FilePaths.Count == 0)
                    return;

                ImageSource = FilePaths[_random.Next(0, FilePaths.Count)];
            }

            PathToDisplay = ImageSource.Replace(Path + "\\", string.Empty);
        }

        protected override int RefreshIntervalInMinutes
        {
            get { return 60; }
        }
    }
}
