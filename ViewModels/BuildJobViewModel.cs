using System;

namespace Display.ViewModels
{
    public abstract class BuildJobViewModel
        : DisplayBaseViewModel
    {
        private const double RefreshUncertaintyPercentage = 0.5;   // +/- 50%
        private const int ApproximateRefreshInterval = 60;

        private static readonly Random Random = new Random();

        public string Id { get; protected set; }

        public string JobName
        {
            get { return PropertyBag.GetAuto<string>(); }
            set { PropertyBag.SetAuto(value); }
        }

        public string CurrentBuild
        {
            get { return PropertyBag.GetAuto<string>(); }
            set { PropertyBag.SetAuto(value); }
        }

        public string LastCompletedBuild
        {
            get { return PropertyBag.GetAuto<string>(); }
            set { PropertyBag.SetAuto(value); }
        }

        public string LastSuccessfulBuild
        {
            get { return PropertyBag.GetAuto<string>(); }
            set { PropertyBag.SetAuto(value); }
        }

        public bool InProgress
        {
            get { return PropertyBag.GetAuto<bool>(); }
            set { PropertyBag.SetAuto(value); }
        }

        public bool? Pass
        {
            get { return PropertyBag.GetAuto<bool?>(); }
            set { PropertyBag.SetAuto(value); }
        }

        public TimeSpan? Duration
        {
            get { return PropertyBag.GetAuto<TimeSpan?>(); }
            set { PropertyBag.SetAuto(value); }
        }

        public DateTime? TimeBuilt
        {
            get { return PropertyBag.GetAuto<DateTime?>(); }
            set { PropertyBag.SetAuto(value); }
        }

        public DateTime? LastChecked
        {
            get { return PropertyBag.GetAuto<DateTime?>(); }
            set { PropertyBag.SetAuto(value); }
        }

        protected override int RefreshIntervalInMinutes
        {
            get
            {
                // We randomly stagger the queries against the build servers to try to distribute load
                var maxOffset = ApproximateRefreshInterval * RefreshUncertaintyPercentage;
                var offset = (Random.NextDouble() * maxOffset * 2) - (maxOffset/2);

                return ApproximateRefreshInterval + (int) offset;
            }
        }
    }
}
