using System.Collections.ObjectModel;

namespace Display.ViewModels.Clocks
{
    public class ClocksDisplayViewModel
        : DisplayBaseViewModel
    {
        public ClocksDisplayViewModel()
        {
            Clocks = new ObservableCollection<ClockViewModel>();

            Title = "World Time";
            Refresh += OnRefresh;
            DisplayShown += OnDisplayShown;
        }

        private void OnDisplayShown()
        {
            OnRefresh();
        }

        private void OnRefresh()
        {
            foreach (var c in Clocks)
            {
                c.Refresh();
            }
        }

        protected override int RefreshIntervalInMinutes { get { return 1; } }

        public ObservableCollection<ClockViewModel> Clocks { get; private set; }
    }
}
