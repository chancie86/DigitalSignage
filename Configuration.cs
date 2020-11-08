using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using Display.Models;
using Display.ViewModels;
using Display.ViewModels.Clocks;
using Display.ViewModels.Twitter;
using Display.ViewModels.Weather;

namespace Display
{
    public class Configuration
    {
        private const string ConfigSchemaManifestResourceName = "Display.XmlSchemas.ConfigSchema.xsd";
        private const string ConfigXmlTargetNamespace = "http://citrix.com/Display/ConfigSchema.xsd";

        private const string JenkinsJobType = "Jenkins";
        private const string TeamCityJobType = "TeamCityBuildJob";
        private const string TeamCityProjectType = "TeamCityProject";

        private const string DisplayIntervalInSecondsAttribute = "DisplayIntervalInSeconds";

        private const string DilbertImageSource = "Dilbert";
        private const string XkcdImageSource = "XKCD";
        private const string CommitStripSource = "CommitStrip";

        public Configuration()
        {
            Roles = new List<Role>();
            SlideShows = new List<SlideShowViewModel>();
            BuildServers = new List<BuildServerViewModel>();
            DailyImages = new List<DailyImageFromRssFeedViewModel>();
            PowerPoints = new List<PowerPointDisplayViewModel>();
            Traffic = new List<GoogleTrafficViewModel>();
            Twitter = new List<TwitterViewModel>();
            Websites = new List<HtmlViewModel>();

            CurrentUser = new User(Environment.UserName, Environment.UserDomainName);
        }

        #region Properties
        public IList<Role> Roles { get; set; }

        public IList<SlideShowViewModel> SlideShows { get; set; }

        public IList<BuildServerViewModel> BuildServers { get; set; }

        public IList<DailyImageFromRssFeedViewModel> DailyImages { get; set; }

        public ClocksDisplayViewModel Clocks { get; set; }

        public MoneypennysViewModel Moneypennys { get; set; }

        public FunCalendarViewModel Calendar { get; set; }
        
        public WeatherDisplayViewModel Weather { get; set; }

        public IList<GoogleTrafficViewModel> Traffic { get; set; }

        public IList<HtmlViewModel> Websites { get; set; }

        public IList<PowerPointDisplayViewModel> PowerPoints { get; set; }

        public IList<TwitterViewModel> Twitter { get; set; }

        public IList<DisplayBaseViewModel> AllDisplays
        {
            get
            {
                var displays = new List<DisplayBaseViewModel>();

                displays.AddRange(SlideShows);
                displays.AddRange(BuildServers);
                displays.AddRange(DailyImages);
                displays.AddRange(PowerPoints);
                displays.AddRange(Websites);
                displays.AddRange(Traffic);
                displays.AddRange(Twitter);

                if (Clocks != null)
                    displays.Add(Clocks);

                if (Moneypennys != null)
                    displays.Add(Moneypennys);

                if (Calendar != null)
                    displays.Add(Calendar);
                
                if (Weather != null)
                    displays.Add(Weather);
                
                return displays;
            }
        }

        private IDictionary<string, Role> RoleIdMap { get; set; }

        private static User CurrentUser { get; set; }
        #endregion

        #region Load Methods
        public static Configuration Load(string path)
        {
            return Load(path, null);
        }

        public static Configuration Load(string path, Configuration oldConfig)
        {
            Log.TraceMsg("Configuration.Load: Path: {0}, oldConfig supplied: {1}", path, oldConfig != null);

            if (!File.Exists(path))
                throw new ArgumentException("Couldn't find config file.", path);

            var xmlReaderSettings = new XmlReaderSettings();
            var assembly = Assembly.GetExecutingAssembly();
            using (var xsd = assembly.GetManifestResourceStream(ConfigSchemaManifestResourceName))
            {
                xmlReaderSettings.Schemas.Add(ConfigXmlTargetNamespace, XmlReader.Create(xsd));
                xmlReaderSettings.ValidationType = ValidationType.Schema;
                xmlReaderSettings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
                xmlReaderSettings.IgnoreComments = true;

                using (var reader = XmlReader.Create(path, xmlReaderSettings))
                {
                    var xmlDoc = new XmlDocument();
                    xmlDoc.Load(reader);

                    var xmlQualifiedName = new XmlQualifiedName(xmlDoc.DocumentElement.LocalName, xmlDoc.DocumentElement.NamespaceURI);
                    var validRoot = xmlDoc.Schemas.GlobalElements.Contains(xmlQualifiedName);

                    if (!validRoot)
                        throw new ArgumentException("Config XML root is invalid");

                    Log.TraceMsg("Configuration.Load: XML file loaded");

                    return ProcessXml(xmlDoc.DocumentElement, oldConfig);
                }
            }
        }
        #endregion

        private static Configuration ProcessXml(XmlNode rootNode, Configuration oldConfig)
        {
            var config = new Configuration();

            ParseRoles(rootNode["Roles"], config);
            ParseSlideshows(rootNode["Slideshows"], config);
            ParseBuildServers(rootNode["BuildServers"], config, oldConfig);
            ParseDailyImages(rootNode["DailyImages"], config, oldConfig);
            ParseClocks(rootNode["Clocks"], config, oldConfig);
            ParseMoneypennys(rootNode["Moneypennys"], config, oldConfig);
            ParsePowerPoints(rootNode["PowerPoints"], config, oldConfig);
            ParseTraffic(rootNode["Traffic"], config);
            ParseWeather(rootNode["Weather"], config, oldConfig);
            ParseWebsites(rootNode["Websites"], config);
            ParseCalendar(rootNode["Calendar"], config, oldConfig);
            ParseTwitter(rootNode["Twitter"], config, oldConfig);

            return config;
        }

        #region Roles
        private static void ParseRoles(XmlNode rolesNode, Configuration config)
        {
            Log.TraceEntry();

            config.RoleIdMap = new Dictionary<string, Role>();

            foreach (XmlNode node in rolesNode.ChildNodes)
            {
                var role = ParseRole(node);
                config.Roles.Add(role);
                config.RoleIdMap[role.Id.ToUpperInvariant()] = role;

                Log.TraceMsg("Added Role '{0}' with users '{1}'", role.Id, string.Join(",", role.Users.Select(user => user.Username)));
            }

            Log.TraceExit();
        }

        private static Role ParseRole(XmlNode roleNode)
        {
            var id = roleNode.Attr("Id");
            var role = new Role(id);

            foreach (XmlNode node in roleNode.ChildNodes)
            {
                role.Users.Add(ParseUsers(node, role));
            }

            return role;
        }

        private static User ParseUsers(XmlNode userNode, Role role)
        {
            var domain = userNode.Attr("Domain");
            return new User(userNode.InnerText, domain);
        }

        #endregion

        #region Slideshows
        private static void ParseSlideshows(XmlNode slideshowNode, Configuration config)
        {
            Log.TraceMsg("Configuration.ParseSlideshows: Parsing slideshows");

            foreach (XmlNode node in slideshowNode.ChildNodes)
            {
                if (!CheckRolesForInclusion(node, config))
                    continue;
                
                var interval = node.Attr(DisplayIntervalInSecondsAttribute);
                var path = node.Attr("Path");

                SlideShowViewModel slideshow;

                var dirInfo = new DirectoryInfo(path);

                if (dirInfo.Exists)
                    slideshow = new SlideShowViewModel(dirInfo.FullName);
                else
                    continue;

                slideshow.DisplayIntervalInSeconds = int.Parse(interval);
                config.SlideShows.Add(slideshow);
            }

            Log.TraceMsg("Configuration.ParseSlideshows: {0} slideshows loaded", config.SlideShows.Count);
        }
        #endregion

        #region Build Servers
        private static void ParseBuildServers(XmlNode buildServersNode, Configuration config, Configuration oldConfig)
        {
            Log.TraceMsg("Configuration.ParseBuildServers");

            var numJobs = 0;
            
            Dictionary<string, BuildJobViewModel> oldJenkinsJobs;
            Dictionary<string, BuildJobViewModel> oldTeamCityJobs;
            Dictionary<string, BuildJobViewModel> oldTeamCityProjects;

            if (oldConfig == null)
            {
                oldJenkinsJobs = oldTeamCityJobs = oldTeamCityProjects = new Dictionary<string, BuildJobViewModel>();
            }
            else
            {
                oldJenkinsJobs = oldConfig.BuildServers.SelectMany(bs => bs.Jobs).Where(job => job is JenkinsBuildJobViewModel).ToDictionary(job => job.Id, job => job);
                oldTeamCityJobs = oldConfig.BuildServers.SelectMany(bs => bs.Jobs).Where(job => job is TeamCityBuildJobViewModel).ToDictionary(job => job.Id, job => job);
                oldTeamCityProjects = oldConfig.BuildServers.SelectMany(bs => bs.Jobs).Where(job => job is TeamCityProjectViewModel).ToDictionary(job => job.Id, job => job);
            }

            var interval = int.Parse(buildServersNode.Attr(DisplayIntervalInSecondsAttribute));

            foreach (XmlNode jobsNode in buildServersNode.ChildNodes)
            {
                if (!CheckRolesForInclusion(jobsNode, config))
                    continue;

                var buildJobs = new List<BuildJobViewModel>();

                // Iterate through the "BuildJobs" nodes
                foreach (XmlNode jobNode in jobsNode)
                {
                    var type = jobNode.Attr("Type");
                    var id = jobNode.Attr("Id");
                    BuildJobViewModel jobViewModel;

                    switch (type)
                    {
                        case JenkinsJobType:
                            if (oldJenkinsJobs.TryGetValue(id, out jobViewModel))
                                buildJobs.Add(oldJenkinsJobs[id]);
                            else
                                buildJobs.Add(new JenkinsBuildJobViewModel(id));

                            numJobs++;
                            break;
                        case TeamCityJobType:
                            if (oldTeamCityJobs.TryGetValue(id, out jobViewModel))
                                buildJobs.Add(oldTeamCityJobs[id]);
                            else
                                buildJobs.Add(new TeamCityBuildJobViewModel(id));

                            numJobs++;
                            break;
                        case TeamCityProjectType:
                            if (oldTeamCityProjects.TryGetValue(id, out jobViewModel))
                                buildJobs.Add(oldTeamCityProjects[id]);
                            else
                                buildJobs.Add(new TeamCityProjectViewModel(id));

                            numJobs++;
                            break;
                    }
                }

                if (buildJobs.Count != 0)
                {
                    var buildServer = new BuildServerViewModel();
                        buildServer.Title = jobsNode.Attr("Title");
                        buildServer.DisplayIntervalInSeconds = interval;
                    buildServer.Jobs.AddRange(buildJobs);

                    config.BuildServers.Add(buildServer);
                }
            }

            Log.TraceMsg("Configuration.ParseBuildServers: {0} build servers loaded with {1} jobs", config.BuildServers.Count, numJobs);
        }
        #endregion

        #region DailyImages
        private static void ParseDailyImages(XmlNode dailyImagesNode, Configuration config, Configuration oldConfig)
        {
            Log.TraceMsg("Configuration.ParseDailyImages");

            foreach (XmlNode node in dailyImagesNode.ChildNodes)
            {
                if (!CheckRolesForInclusion(node, config))
                    continue;

                var interval = int.Parse(node.Attr(DisplayIntervalInSecondsAttribute));
                var source = node.Attr("Source");

                DailyImageFromRssFeedViewModel feed = null;

                switch (source)
                {
                    case DilbertImageSource:
                        feed = GetFeed<DilbertDailyImageFromRssFeedViewModel>(oldConfig);
                        break;
                    case XkcdImageSource:
                        feed = GetFeed<XkcdDailyImageFromRssFeedViewModel>(oldConfig);
                        break;
                    case CommitStripSource:
                        feed = GetFeed<CommitStripDailyImageFromRssFeedViewModel>(oldConfig);
                        break;
                }

                if (feed == null)
                    continue;

                feed.DisplayIntervalInSeconds = interval;
                config.DailyImages.Add(feed);
            }

            Log.TraceMsg("Configuration.ParseDailyImages: {0} DailyImage feeds loaded", config.DailyImages.Count);
        }

        private static DailyImageFromRssFeedViewModel GetFeed<T>(Configuration oldConfig)
            where T : DailyImageFromRssFeedViewModel, new()
        {
            if (oldConfig == null)
            {
                Log.TraceMsg("Configuration.GetFeed: Old configuration not supplied, creating new feed of type '{0}'", typeof(T));
                return new T();
            }

            var feed = oldConfig.DailyImages.FirstOrDefault(di => di is T);

            if (feed == null)
            {
                Log.TraceMsg("Configuration.GetFeed: Feed not found in previous config, loading new");
                feed = new T();
            }
            else
            {
                Log.TraceMsg("Configuration.GetFeed: Feed found in previous config, reusing");
            }

            return feed;
        }
        #endregion

        #region Clocks
        private static void ParseClocks(XmlNode clocksNode, Configuration config, Configuration oldConfig)
        {
            Log.TraceEntry();

            var interval = int.Parse(clocksNode.Attr(DisplayIntervalInSecondsAttribute));

            var clocks = new List<ClockViewModel>();

            foreach (XmlNode node in clocksNode.ChildNodes)
            {
                if (!CheckRolesForInclusion(node, config))
                    continue;

                var name = node.Attr("Name");
                var timeZone = node.Attr("TimeZone");
                var clock = new ClockViewModel(name) {TimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZone)};
                clock.Refresh();
                clocks.Add(clock);
            }

            if (clocks.Count == 0)
            {
                Log.TraceMsg("Configuration.ParseClocks: No Clocks loaded");
            }
            else
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    ClocksDisplayViewModel clocksDisplayViewModel;
                    if (oldConfig != null
                        && oldConfig.Clocks != null)
                    {
                        Log.TraceMsg("Configuration.ParseClocks: Reusing ClocksDisplayViewModel from old config");
                        clocksDisplayViewModel = oldConfig.Clocks;
                        clocksDisplayViewModel.Clocks.Clear();
                    }
                    else
                    {
                        Log.TraceMsg("Configuration.ParseClocks: Creating new ClocksDisplayViewModel");
                        clocksDisplayViewModel = new ClocksDisplayViewModel();   
                    }

                    clocksDisplayViewModel.DisplayIntervalInSeconds = interval;
                    clocksDisplayViewModel.Clocks.AddRange(clocks);
                    config.Clocks = clocksDisplayViewModel;
                });
                
                Log.TraceMsg("Configuration.ParseClocks: {0} Clocks loaded", config.Clocks.Clocks.Count);
            }

            Log.TraceExit();
        }
        #endregion

        #region Moneypennys
        private static void ParseMoneypennys(XmlNode moneypennysNode, Configuration config, Configuration oldConfig)
        {
            Log.TraceEntry();

            if (moneypennysNode == null)
            {
                Log.TraceMsg("Configuration.ParseMoneypennys: not required");
                return;
            }

            if (oldConfig != null && oldConfig.Moneypennys != null)
            {
                Log.TraceMsg("Configuration.ParseMoneypennys: Reusing Moneypennys view model from old config");
                config.Moneypennys = oldConfig.Moneypennys;
            }
            else
            {
                Log.TraceMsg("Configuration.ParseMoneypennys: Creating new Moneypennys view model");
                config.Moneypennys = new MoneypennysViewModel();
            }

            Log.TraceExit();
        }
        #endregion

        #region PowerPoints
        private static void ParsePowerPoints(XmlNode powerPointsNode, Configuration config, Configuration oldConfig)
        {
            Log.TraceEntry();

            foreach (XmlNode node in powerPointsNode.ChildNodes)
            {
                if (!CheckRolesForInclusion(node, config))
                    continue;

                var interval = int.Parse(node.Attr(DisplayIntervalInSecondsAttribute));
                var path = node.Attr("Path");
                config.PowerPoints.Add(GetPowerPoint(oldConfig, interval, path));
            }

            Log.TraceExit();
        }

        private static PowerPointDisplayViewModel GetPowerPoint(Configuration oldConfig, int interval, string path)
        {
            if (oldConfig == null)
            {
                Log.TraceMsg("Configuration.GetPowerPoint: Old configuration not supplied, creating new view model");
                return new PowerPointDisplayViewModel(path)
                {
                    DisplayIntervalInSeconds = interval
                };
            }

            var viewModel = oldConfig.PowerPoints.FirstOrDefault(pp => string.Equals(pp.FilePath, path, StringComparison.OrdinalIgnoreCase));

            if (viewModel == null)
            {
                Log.TraceMsg("Configuration.GetPowerPoint: View model not found in previous config, loading new at: '{0}'", path);
                viewModel = new PowerPointDisplayViewModel(path)
                {
                    DisplayIntervalInSeconds = interval
                };
            }
            else
            {
                Log.TraceMsg("Configuration.GetPowerPoint: Feed found in previous config, reusing");
            }

            return viewModel;
        }
        #endregion

        #region Traffic
        private static void ParseTraffic(XmlNode trafficNode, Configuration config)
        {
            Log.TraceEntry();

            if (trafficNode == null)
            {
                Log.TraceMsg("No traffic nodes found");
                return;
            }

            foreach (XmlNode node in trafficNode.ChildNodes)
            {
                if (!CheckRolesForInclusion(node, config))
                    continue;

                var interval = int.Parse(node.Attr(DisplayIntervalInSecondsAttribute));
                var placeId = node.Attr("GooglePlaceId");
                var title = node.Attr("Title");

                var location = new GoogleTrafficViewModel(placeId)
                {
                    Title = title,
                    DisplayIntervalInSeconds = interval
                };

                config.Traffic.Add(location);
            }

            Log.TraceExit();
        }
        #endregion

        #region Weather
        private static void ParseWeather(XmlNode weatherNode, Configuration config, Configuration oldConfig)
        {
            Log.TraceEntry();

            if (weatherNode == null)
            {
                Log.TraceMsg("Configuration.ParseWeather: not required");
                return;
            }

            var location = weatherNode.Attr("Location");

            WeatherDisplayViewModel weather;

            if (oldConfig != null
                && oldConfig.Weather != null)
            {
                weather = oldConfig.Weather;
                weather.UpdateLocation(location);
            }
            else
            {
                weather = new WeatherDisplayViewModel(location);
            }


            weather.Units = string.Equals(weatherNode.Attr("Units"), "Metric", StringComparison.OrdinalIgnoreCase)
                ? Units.Metric
                : Units.Imperial;
            weather.DisplayIntervalInSeconds = int.Parse(weatherNode.Attr(DisplayIntervalInSecondsAttribute));

            config.Weather = weather;

            Log.TraceExit();
        }
        #endregion

        #region Websites
        private static void ParseWebsites(XmlNode websitesNode, Configuration config)
        {
            Log.TraceEntry();

            foreach (XmlNode node in websitesNode.ChildNodes)
            {
                if (!CheckRolesForInclusion(node, config))
                    continue;

                var url = node.Attr("Url");
                var interval = int.Parse(node.Attr(DisplayIntervalInSecondsAttribute));
                var isChrome = string.Equals(node.Attr("Browser"), "Chrome");

                var viewModel = isChrome
                    ? new ChromeHtmlViewModel(url)
                    : new HtmlViewModel(url);
                viewModel.DisplayIntervalInSeconds = interval;

                config.Websites.Add(viewModel);
            }

            Log.TraceExit();
        }
        #endregion

        #region Calendar
        private static void ParseCalendar(XmlNode calendarNode, Configuration config, Configuration oldConfig)
        {
            Log.TraceEntry();

            if (calendarNode == null)
            {
                Log.TraceMsg("Configuration.ParseCalendar: not required");
                return;
            }

            if (oldConfig != null && oldConfig.Calendar != null)
            {
                Log.TraceMsg("Configuration.ParseCalendar: Reusing calendar view model from old config");
                config.Calendar = oldConfig.Calendar;
            }
            else
            {
                Log.TraceMsg("Configuration.ParseCalendar: Creating new calendar view model");
                config.Calendar = new FunCalendarViewModel();
            }

            Log.TraceExit();
        }

        #endregion

        #region Twitter
        private static void ParseTwitter(XmlNode twitterNode, Configuration config, Configuration oldConfig)
        {
            Log.TraceEntry();

            foreach (XmlNode node in twitterNode.ChildNodes)
            {
                if (!CheckRolesForInclusion(node, config))
                    continue;

                var queryText = node.Attr("Value");
                var oldTwitter = oldConfig == null
                    ? null
                    : oldConfig.Twitter.FirstOrDefault(
                        t => string.Equals(t.Query, queryText, StringComparison.OrdinalIgnoreCase));

                var twitter = oldTwitter ?? new TwitterViewModel(queryText);
                twitter.DisplayIntervalInSeconds = int.Parse(node.Attr(DisplayIntervalInSecondsAttribute));

                Log.TraceMsg("Added Twitter display with query '{0}'", queryText);
                config.Twitter.Add(twitter);
            }

            Log.TraceExit();
        }
        #endregion

        private static bool CheckRolesForInclusion(XmlNode node, Configuration config)
        {
            string roleIds;
            try
            {
                roleIds = node.Attr("Roles");
            }
            catch (Exception)
            {
                Log.TraceMsg("CheckRolesForInclusion: No roles specified");
                return true;
            }

            var splitRoles = roleIds.Split(';');

            foreach (var roleId in splitRoles)
            {
                Role role;
                if (!config.RoleIdMap.TryGetValue(roleId.ToUpperInvariant(), out role))
                {
                    throw new Exception(string.Format("The specified role '{0}' was not found in the defined roles", roleId));
                }

                if (
                    role.Users.Any(
                        user =>
                            string.Equals(user.Username, CurrentUser.Username,
                                StringComparison.InvariantCultureIgnoreCase)
                            && string.Equals(user.Domain, CurrentUser.Domain, StringComparison.InvariantCultureIgnoreCase)))
                {
                    Log.TraceMsg("CheckRolesForInclusion: true");
                    return true;
                }
                    
            }

            Log.TraceMsg("CheckRolesForInclusion: false");
            return false;
        }
    }
}
