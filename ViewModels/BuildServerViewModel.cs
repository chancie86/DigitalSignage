using System.Collections.ObjectModel;

namespace Display.ViewModels
{
    public class BuildServerViewModel
        : DisplayBaseViewModel
    {
        public BuildServerViewModel()
        {
            Jobs = new ObservableCollection<BuildJobViewModel>();
            DisplayShown += OnDisplayShown;
            Title = "Latest Build Job Results";
        }

        private void OnDisplayShown()
        {
            foreach (var job in Jobs)
            {
                job.RaiseDisplayShown();
            }
        }

        public ObservableCollection<BuildJobViewModel> Jobs { get; private set; }
    }
}
