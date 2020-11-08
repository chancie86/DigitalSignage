using System;
using System.Runtime.Serialization;

namespace Display.ViewModels
{
    public class JenkinsBuildJobViewModel
        : BuildJobViewModel
    {
        private const string BaseUrl = "http://jenkinsserver.eng.citrite.net/";
        private const string JobUrlFormat = "job/{0}/api/json";
        private const string DetailsUrlFormat = "job/{0}/{1}/api/json?tree=result,duration,estimatedDuration,timestamp";
        private const string NotBuiltResult = "NOT_BUILT";

        private readonly RestClient _restClient;

        public JenkinsBuildJobViewModel(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(name);

            Id = name;
            JobName = "J: " + name;

            _restClient = new RestClient(RestClient.JsonType);
            Refresh += OnRefresh;
            DisplayShown += OnDisplayShown;
        }

        private void OnDisplayShown()
        {
            if (!LastChecked.HasValue)
            {
                OnRefresh();
            }
        }

        private void OnRefresh()
        {
            try
            {
                var jobMethod = string.Format(JobUrlFormat, Id);
                var job = _restClient.MakeRequest<Job>(BaseUrl, jobMethod, null, null);

                LastChecked = DateTime.Now;
                CurrentBuild = job.LastBuild.ToString();
                LastSuccessfulBuild = job.LastSuccessfulBuild == null ? string.Empty : job.LastSuccessfulBuild.ToString();
                LastCompletedBuild = job.LastCompletedBuild.Number.ToString();

                if (job.LastSuccessfulBuild == null)
                {
                    if (job.LastCompletedBuild != null)
                    {
                        Pass = false;
                    }
                    else
                    {
                        Pass = null;
                    }
                }
                else
                {
                    Pass = job.LastCompletedBuild.Number == job.LastSuccessfulBuild.Number || WasLastActualBuildSuccess(job);
                }

                InProgress = job.LastBuild.Number != job.LastCompletedBuild.Number;

                var detailsMethod = string.Format(DetailsUrlFormat, Id, CurrentBuild);
                var details = _restClient.MakeRequest<BuildDetails>(BaseUrl, detailsMethod, null, null);

                var durationTicks = (InProgress ? details.EstimatedDuration : details.Duration) * 10000;
                Duration = new TimeSpan(durationTicks);
                TimeBuilt = TimeHelpers.TimeFromEpochTimestamp(details.Timestamp);
            }
            catch (Exception ex)
            {
                Log.TraceErr("JenkinsBuildJobViewModel.OnRefresh: Error retrieving results for '{0}'. {1}", Id, ex.ToString());
                LastChecked = null;
                Pass = null;
            }
        }

        private bool WasLastActualBuildSuccess(Job job)
        {
            var buildToCheck = job.LastCompletedBuild.Number;
            while (buildToCheck > job.LastSuccessfulBuild.Number)
            {
                var detailsMethod = string.Format(DetailsUrlFormat, Id, buildToCheck);
                var details = _restClient.MakeRequest<BuildDetails>(BaseUrl, detailsMethod, null, null);

                if (!string.Equals(details.Result, NotBuiltResult, StringComparison.Ordinal))
                    return false;

                buildToCheck--;
            }

            return true;
        }

        [DataContract]
        public sealed class Job
        {
            [DataMember(Name = "lastBuild")]
            public Build LastBuild { get; set; }

            [DataMember(Name = "lastSuccessfulBuild")]
            public Build LastSuccessfulBuild { get; set; }

            [DataMember(Name = "lastCompletedBuild")]
            public Build LastCompletedBuild { get; set; }
        }

        [DataContract]
        public sealed class Build
        {
            [DataMember(Name = "number")]
            public int Number { get; set; }

            public override string ToString()
            {
                return Number.ToString();
            }
        }

        [DataContract]
        public sealed class BuildDetails
        {
            [DataMember(Name = "duration")]
            public long Duration { get; set; }

            [DataMember(Name = "estimatedDuration")]
            public long EstimatedDuration { get; set; }

            [DataMember(Name = "timestamp")]
            public long Timestamp { get; set; }

            [DataMember(Name = "result")]
            public string Result { get; set; }
        }
    }
}
