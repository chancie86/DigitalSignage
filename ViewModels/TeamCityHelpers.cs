using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Display.ViewModels
{
    internal static class TeamCityHelpers
    {
        private const string BaseUrl = "https://ftltc01.eng.citrite.net/";
        private const string ProjectsFormat = "guestAuth/app/rest/projects/id:{0}";
        private const string BuildsFormat = "guestAuth/app/rest/builds";
        private const string BuildTypesFormat = "guestAuth/app/rest/buildTypes/id:{0}";

        private const string LocatorQuery = "locator";

        private const string SuccessStatus = "SUCCESS";
        private const string RunningState = "running";

        private static readonly RestClient _restClient = new RestClient(RestClient.JsonType, RestClient.JsonType);

        public static string FindFriendlyName(string buildTypeId)
        {
            var buildType = _restClient.MakeRequest<BuildType>(BaseUrl,
                string.Format(CultureInfo.InvariantCulture, BuildTypesFormat, buildTypeId),
                null, null);

            if (buildType != null)
            {
                if (buildType.Project != null)
                {
                    return string.Format(CultureInfo.InvariantCulture, "{0} ({1})", buildType.Name,
                        buildType.Project.ParentProjectId);
                }

                return buildType.Name;
            }

            return buildTypeId;
        }

        public static Builds FindBuilds(string buildTypeId)
        {
            Log.TraceMsg("Finding TeamCity build '{0}'", buildTypeId);

            var locator = LocatorBuilder(new Dictionary<string, string> {
                        { "buildType", buildTypeId },
                        { "count", "1000" }
                    });

            var builds = _restClient.MakeRequest<Builds>(BaseUrl, BuildsFormat, null, new Dictionary<string, string> { { "locator", locator } });

            return builds;
        }

        public static Build FindLastSuccessfulBuild(Build[] builds)
        {
            if (builds == null)
                return null;

            return builds.FirstOrDefault(b => string.Equals(SuccessStatus, b.Status));
        }

        public static BuildDetails FindBuildInfo(Build build)
        {
            if (build == null)
                throw new ArgumentNullException("build");

            return _restClient.MakeRequest<BuildDetails>(BaseUrl, build.Href, null, null);
        }

        public static Build FindRunningBuild(string buildTypeId)
        {
            var locator = LocatorBuilder(new Dictionary<string, string> {
                        { RunningState, "true" }
                    });

            var builds = _restClient.MakeRequest<Builds>(BaseUrl, BuildsFormat, null, new Dictionary<string, string> { { LocatorQuery, locator } });

            return builds.Build.FirstOrDefault(b => string.Equals(b.BuildTypeId, buildTypeId, StringComparison.OrdinalIgnoreCase));
        }

        public static void FindProjectStatus(TeamCityProjectViewModel model)
        {
            Log.TraceEntry();
            
            Log.TraceMsg("Finding TeamCity project '{0}'", model.Id);

            var project = _restClient.MakeRequest<Project>(BaseUrl,
                string.Format(CultureInfo.InvariantCulture, ProjectsFormat, model.Id),
                null, null);

            if (!string.IsNullOrEmpty(project.Name))
                model.JobName = string.Format("{0} ({1})", project.Name, project.ParentProjectId);

            if (project.BuildTypes.BuildType.Length == 0)
                return;

            var objectLock = new object();
            var totalDuration = new TimeSpan(0);
            var inProgress = false;
            var overallPass = true;

            Parallel.ForEach(project.BuildTypes.BuildType, buildType =>
            {
                var builds = FindBuilds(buildType.Id);

                if (builds == null
                    || builds.Count == 0
                    || builds.Build.Length == 0)
                    return;

                var isLayout = buildType.Id.EndsWith("layout", StringComparison.InvariantCultureIgnoreCase);
                var lastBuild = builds.Build.First();
                var lastSuccessfulBuild = FindLastSuccessfulBuild(builds.Build);

                var duration = TimeSpan.Zero;
                var pass = false;

                if (lastSuccessfulBuild == null)
                {
                    model.LastSuccessfulBuild = "-";
                }
                else
                {
                    var successFullBuildInfo = FindBuildInfo(lastSuccessfulBuild);
                    duration = (ParseTime(successFullBuildInfo.FinishDate) - ParseTime(successFullBuildInfo.StartDate));
                    pass = lastSuccessfulBuild.Number == lastBuild.Number;

                    if (isLayout)
                    {
                        model.LastSuccessfulBuild = lastSuccessfulBuild.Number.ToString();
                        model.TimeBuilt = ParseTime(successFullBuildInfo.FinishDate);
                    }
                }

                var runningBuild = FindRunningBuild(buildType.Id);

                if (isLayout)
                {
                    model.LastCompletedBuild = lastBuild.Number.ToString();
                    model.CurrentBuild = (runningBuild == null ? lastBuild.Number : runningBuild.Number).ToString();
                }
                
                lock (objectLock)
                {
                    totalDuration += duration;
                    overallPass &= pass;
                    inProgress |= runningBuild != null;
                }
            });

            model.Duration = totalDuration;
            model.Pass = overallPass;
            model.InProgress = inProgress;

            Log.TraceExit();
        }

        #region Helpers
        public static DateTime ParseTime(string dateTimeString)
        {
            // TeamCity returns date/time in this format: 20150908T103250-0400
            // Convert it into a form that the DateTime class likes... target yyyy-mm-ddThh:mm:ss-hh:mm

            var sb = new StringBuilder(dateTimeString.Substring(0, 4)); // Year
            sb.Append('-');
            sb.Append(dateTimeString.Substring(4, 2));  // Month
            sb.Append('-');
            sb.Append(dateTimeString.Substring(6, 5));  // Day + Hour
            sb.Append(':');
            sb.Append(dateTimeString.Substring(11, 2));  // Minute
            sb.Append(':');
            sb.Append(dateTimeString.Substring(13, 5));  // Second + hour offset
            sb.Append(':');
            sb.Append(dateTimeString.Substring(18, 2));  // Minute offset

            return DateTime.Parse(sb.ToString());
        }

        private static string LocatorBuilder(IDictionary<string, string> criteria)
        {
            var locator = new StringBuilder();

            var i = 1;
            foreach (var kvp in criteria)
            {
                locator.Append(kvp.Key);
                locator.Append(':');
                locator.Append(kvp.Value);

                if (i++ != criteria.Count)
                    locator.Append(',');
            }

            return locator.ToString();
        }
        #endregion

        #region JSON contract
        [DataContract]
        public sealed class BuildTypes
        {
            [DataMember(Name = "buildType")]
            public BuildType[] BuildType { get; set; }
        }

        [DataContract]
        public sealed class BuildType
        {
            [DataMember(Name = "id")]
            public string Id { get; set; }

            [DataMember(Name = "name")]
            public string Name { get; set; }

            [DataMember(Name = "project")]
            public Project Project { get; set; }
        }

        [DataContract]
        public sealed class Builds
        {
            [DataMember(Name = "build")]
            public Build[] Build { get; set; }

            [DataMember(Name = "count")]
            public int Count { get; set; }
        }

        [DataContract]
        public sealed class Build
        {
            [DataMember(Name = "status")]
            public string Status { get; set; }

            [DataMember(Name = "buildTypeId")]
            public string BuildTypeId { get; set; }

            [DataMember(Name = "number")]
            public string Number { get; set; }

            [DataMember(Name = "href")]
            public string Href { get; set; }
        }

        [DataContract]
        public sealed class BuildDetails
        {
            [DataMember(Name = "startDate")]
            public string StartDate { get; set; }

            [DataMember(Name = "finishDate")]
            public string FinishDate { get; set; }
        }

        [DataContract]
        public sealed class Project
        {
            [DataMember(Name = "name")]
            public string Name { get; set; }

            [DataMember(Name = "parentProjectId")]
            public string ParentProjectId { get; set; }

            [DataMember(Name = "buildTypes")]
            public BuildTypes BuildTypes { get; set; }
        }
        #endregion
    }
}
