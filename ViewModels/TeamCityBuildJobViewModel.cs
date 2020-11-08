using System;
using System.Linq;

namespace Display.ViewModels
{
    public class TeamCityBuildJobViewModel
        : BuildJobViewModel
    {
        public TeamCityBuildJobViewModel(string projectId)
        {
            if (string.IsNullOrWhiteSpace(projectId))
                throw new ArgumentNullException(projectId);

            Id = projectId;
            JobName = Id;  // This will get replaced after the first time the build job info is sync'd
            
            LastCompletedBuild = "-";
            LastSuccessfulBuild = "-";
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
                JobName = TeamCityHelpers.FindFriendlyName(Id);
                FindLatestBuildInfo();
                FindCurrentBuild();

                LastChecked = DateTime.Now;
            }
            catch (Exception ex)
            {
                Log.TraceErr("TeamCityBuildJobViewModel.OnRefresh: Error retrieving results for '{0}'. {1}", Id, ex.ToString());

                LastChecked = null;
                Pass = null;
            }
        }

        private void FindLatestBuildInfo()
        {
            Log.TraceEntry();

            var builds = TeamCityHelpers.FindBuilds(Id);

            if (builds == null
                || builds.Count == 0
                || builds.Build.Length == 0)
                return;

            var lastCompletedBuild = builds.Build.First();

            LastCompletedBuild = ShortenedBuildText(lastCompletedBuild.Number);
            var lastSuccessfulBuild = TeamCityHelpers.FindLastSuccessfulBuild(builds.Build);
            LastSuccessfulBuild = lastSuccessfulBuild == null ? "-" : ShortenedBuildText(lastSuccessfulBuild.Number);

            var buildDetails = TeamCityHelpers.FindBuildInfo(lastCompletedBuild);
            TimeBuilt = TeamCityHelpers.ParseTime(buildDetails.FinishDate);
            Duration = TimeBuilt - TeamCityHelpers.ParseTime(buildDetails.StartDate);
            LastChecked = DateTime.Now;

            Pass = string.Equals(LastSuccessfulBuild, LastCompletedBuild);

            Log.TraceExit();
        }

        private string ShortenedBuildText(string buildNumber)
        {
            var splitBuildNumber = buildNumber.Split(new [] { ' ' }, 2);
            buildNumber = splitBuildNumber[0];

            return buildNumber.Substring(0, Math.Min(buildNumber.Length, 11));
        }

        private void FindCurrentBuild()
        {
            var build = TeamCityHelpers.FindRunningBuild(Id);

            if (build == null)
            {
                InProgress = false;
                CurrentBuild = LastCompletedBuild;
            }
            else
            {
                InProgress = true;
                CurrentBuild = build.Number;
            }
        }
    }
}
