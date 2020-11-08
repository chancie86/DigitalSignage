using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Schema;

namespace Display.ViewModels
{
    public class MoneypennysViewModel
        : DisplayBaseViewModel
    {
        private const string MenuFileName = "Moneypennys.xml";
        private const string MoneypennysSchemaManifestResourceName = "Display.XmlSchemas.MoneypennysSchema.xsd";
        private const string MoneypennysXmlTargetNamespace = "http://citrix.com/Display/MoneypennysSchema.xsd";

        private readonly ObservableCollection<string> _writableDayMenu;

        public MoneypennysViewModel()
        {
            _writableDayMenu = new ObservableCollection<string>();
            DayMenu = new ReadOnlyObservableCollection<string>(_writableDayMenu);

            LoadMenu();

            Title = "Moneypennys";

            Refresh += LoadMenu;
        }

        public string Day
        {
            get { return PropertyBag.GetAuto<string>(); }
            set { PropertyBag.SetAuto(value); }
        }

        public ReadOnlyObservableCollection<string> DayMenu { get; private set; }

        protected override int RefreshIntervalInMinutes { get { return 60; } }  // Check every hour

        private void LoadMenu()
        {
            try
            {
                LoadXml();
            }
            catch (Exception ex)
            {
                Day = "Couldn't load menu. " + ex.Message;

                Invoke(() => _writableDayMenu.Clear());

                Log.TraceErr("Unable to load menu. {0}", ex.ToString());
            }
        }

        private void LoadXml()
        {
            Log.TraceMsg("MoneypennysViewModel.LoadXml");

            if (!File.Exists(MenuFileName))
            {
                Log.TraceErr("MoneypennysViewModel.LoadXml: Couldn't find xml file");
                throw new ArgumentException("Couldn't find config file.", MenuFileName);
            }

            var lastModified = File.GetLastWriteTimeUtc(MenuFileName);
            if (DateTime.UtcNow - lastModified > TimeSpan.FromDays(5))
            {
                // If the menu wasn't updated in the last 5 days we disable this display because
                // we'd be showing an out of date menu.
                IsVisible = false;
                return;
            }

            IsVisible = true;

            var xmlReaderSettings = new XmlReaderSettings();
            var assembly = Assembly.GetExecutingAssembly();
            using (var xsd = assembly.GetManifestResourceStream(MoneypennysSchemaManifestResourceName))
            {
                xmlReaderSettings.Schemas.Add(MoneypennysXmlTargetNamespace, XmlReader.Create(xsd));
                xmlReaderSettings.ValidationType = ValidationType.Schema;
                xmlReaderSettings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
                xmlReaderSettings.IgnoreComments = true;

                using (var reader = XmlReader.Create(MenuFileName, xmlReaderSettings))
                {
                    var xmlDoc = new XmlDocument();
                    xmlDoc.Load(reader);

                    var xmlQualifiedName = new XmlQualifiedName(xmlDoc.DocumentElement.LocalName, xmlDoc.DocumentElement.NamespaceURI);
                    var validRoot = xmlDoc.Schemas.GlobalElements.Contains(xmlQualifiedName);

                    if (!validRoot)
                        throw new ArgumentException("XML root is invalid");

                    Log.TraceMsg("MoneypennysViewModel.LoadXml: XML file loaded");

                    ProcessXml(xmlDoc.DocumentElement);
                }
            }
        }

        private void ProcessXml(XmlNode rootNode)
        {
            // Read 10 hours ahead so that the menu changes to the next day after 2pm (after lunch serving hours)
            var today = DateTime.Now.AddHours(10).ToString("dddd", CultureInfo.CreateSpecificCulture("en-US"));
            var node = rootNode[today];

            var menu = new List<string>();

            if (node == null)
                return;

            using (var stringReader = new StringReader(node.InnerText))
            {
                string line = null;

                do
                {
                    line = stringReader.ReadLine();
                    menu.Add(string.IsNullOrWhiteSpace(line) ? string.Empty : line.Trim());
                } while (line != null);
            }

            Day = today;
            
            Invoke(() =>
            {
                _writableDayMenu.Clear();
                _writableDayMenu.AddRange(menu);
            });
        }
    }
}
