using System;
using System.IO;
using System.Reflection;

namespace Display.ViewModels
{
    public class GoogleTrafficViewModel
        : ChromeHtmlViewModel
    {
        private readonly string _filePath;

        public GoogleTrafficViewModel(string placeId)
            : base(null)
        {
            try
            {
                var directory = _filePath = Path.Combine(WebHelpers.ApplicationTempDirectory, "GoogleMaps");

                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                _filePath = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".html");
                
                GenerateHtml(placeId);

                Address = new Uri(_filePath).AbsoluteUri;
            }
            catch
            {
                Log.TraceErr("Unable to write Google Maps html file");
                IsVisible = false;
            };
        }

        /// <summary>
        /// When the map is displayed it will reach out for the data via Google, so we don't need to do any refreshing.
        /// </summary>
        protected override int RefreshIntervalInMinutes { get { return 0; } }

        private void GenerateHtml(string placeId)
        {
            var assembly = Assembly.GetExecutingAssembly();

            using (var manifestStream = assembly.GetManifestResourceStream("Display.ViewModels.GoogleMap.html"))
            {
                if (manifestStream == null)
                    throw new Exception("Unable to generate html");

                using (var streamWriter = new StreamWriter(_filePath))
                {
                    using (var streamReader = new StreamReader(manifestStream))
                    {
                        for (var line = streamReader.ReadLine(); line != null; line = streamReader.ReadLine())
                        {
                            line = line.Replace("/*#PLACE_ID#*/", placeId);
                            line = line.Replace("/*#API_KEY#*/", WebHelpers.GoogleWebApiKey);
                            streamWriter.WriteLine(line);
                        }
                    }
                }
            }

            Log.TraceMsg("HTML written to '{0}'", _filePath);
        }
    }
}
