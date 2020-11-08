using System;

namespace Display.ViewModels
{
    public class TeamCityProjectViewModel
        : BuildJobViewModel
    {
        public TeamCityProjectViewModel(string projectId)
        {
            if (string.IsNullOrWhiteSpace(projectId))
                throw new ArgumentNullException(projectId);

            Id = projectId;
            JobName = Id;  // This will get replaced after the first time the build job info is sync'd

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
                TeamCityHelpers.FindProjectStatus(this);
                LastChecked = DateTime.Now;
            }
            catch (Exception ex)
            {
                Log.TraceErr("TeamCityProjectViewModel.OnRefresh: Error retrieving results for '{0}'. {1}", Id, ex.ToString());
                Pass = null;
            }
        }
    }
}
