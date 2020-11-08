using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;

namespace Display.ViewModels
{
    public abstract class DailyImageFromRssFeedViewModel
        : SlideShowViewModel
    {
        private const string Rss20XmlSchemaManifestResourceName = "Display.XmlSchemas.RSS20.xsd";
        private const string Rss20XmlTargetNamespace = "http://citrix.com/Display/Rss20Schema.xsd";

        private string _fileName;
        private Item _itemCurrentlyDisplayed;

        protected DailyImageFromRssFeedViewModel(string subFolder)
            : base(System.IO.Path.Combine(WebHelpers.ApplicationTempDirectory, "DailyImages", subFolder))
        {
            if (!Directory.Exists(Path))
                Directory.CreateDirectory(Path);

            IsVisible = false;
        }

        public abstract string FileExtension { get; }

        public abstract string Address { get; }

        protected override int RefreshIntervalInMinutes { get { return 60 * 6; } }

        protected abstract Item ParseXml(XmlNode rssNode);

        protected sealed override void LoadImageFilePaths()
        {
            PathToDisplay = "Loading RSS feed...";

            try
            {
                var rssFeed = LoadRssFeed();
                ProcessRssResponse(rssFeed);
                IsVisible = true;
            }
            catch (Exception ex)
            {
                Log.TraceErr("Error reading RSS feed.{0}{1}",Environment.NewLine, ex.ToString());
                PathToDisplay = "Error reading RSS feed. " + ex.Message;
                IsVisible = false;
            }

            ImageSource = null;

            // Load the image file.
            lock (FilePathsLock)
            {
                try
                {
                    var path = System.IO.Path.Combine(Path, _fileName);

                    if (File.Exists(path))
                    {
                        FilePaths = new[] { path };
                        FilesAvailable = true;
                    }
                    else
                    {
                        FilePaths = null;
                        FilesAvailable = false;
                    }
                }
                catch (Exception ex)
                {
                    FilePaths = null;
                    FilesAvailable = false;
                    PathToDisplay = "Couldn't load image files. " + ex.Message;
                }
            }

            TryCleanUpOldFiles();
        }

        protected override void DisplayShownHandler()
        {
            base.DisplayShownHandler();

            if (_itemCurrentlyDisplayed != null)
                PathToDisplay = _itemCurrentlyDisplayed.Title + " (" + _itemCurrentlyDisplayed.Date.ToString("D") + ")";
        }

        private void TryCleanUpOldFiles()
        {
            var allFiles = Directory.EnumerateFiles(Path, "*", SearchOption.AllDirectories).ToArray();
            foreach (var filePath in allFiles)
            {
                if (!filePath.EndsWith("\\" + _fileName))
                {
                    try
                    {
                        File.Delete(filePath);
                    }
                    catch (Exception)
                    {
                        // Boo! Never mind, just try again next time.
                    }
                }
            }
        }

        private void ProcessRssResponse(XmlDocument rssFeed)
        {
            var newestItem = ParseXml(rssFeed.DocumentElement);

            if (_itemCurrentlyDisplayed != null
                && _itemCurrentlyDisplayed.Date >= newestItem.Date)
                return;

            var newFileName = Guid.NewGuid().ToString("N") + "." + (string.IsNullOrWhiteSpace(FileExtension) ? "jpg" : FileExtension);

            // Download the file
            using (var client = new WebClient())
            {
                client.DownloadFile(newestItem.ImageAddress, System.IO.Path.Combine(Path, newFileName));
            }

            _fileName = newFileName;

            _itemCurrentlyDisplayed = newestItem;
        }

        private XmlDocument LoadRssFeed()
        {
            var webRequest = WebRequest.Create(Address) as HttpWebRequest;

            if (webRequest == null)
                throw new Exception("Couldn't create http request");

            using (var webResponse = (HttpWebResponse)webRequest.GetResponse())
            {
                if (webResponse.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception(string.Format(CultureInfo.InvariantCulture,
                        "HTTP request failed. Request: {0}, Status Code: {1}, Status Description: {2}",
                        Address,
                        webResponse.StatusCode,
                        webResponse.StatusDescription));
                }

                using (var responseStream = webResponse.GetResponseStream())
                {
                    if (responseStream == null)
                        throw new Exception("Invalid response");

                    var xmlReaderSettings = new XmlReaderSettings();
                    var assembly = Assembly.GetExecutingAssembly();
                    using (var xsd = assembly.GetManifestResourceStream(Rss20XmlSchemaManifestResourceName))
                    {
                        xmlReaderSettings.Schemas.Add(Rss20XmlTargetNamespace, XmlReader.Create(xsd));
                        xmlReaderSettings.ValidationType = ValidationType.Schema;
                        xmlReaderSettings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
                    }

                    var xmlReader = XmlReader.Create(responseStream, xmlReaderSettings);
                    var xmlDoc = new XmlDocument();
                    xmlDoc.Load(xmlReader);

                    return xmlDoc;
                }
            }
        }
    }
}
