using System;
using WpfUtils;

namespace Display.ViewModels.Clocks
{
    public class ClockViewModel
        : BaseViewModel
    {
        public ClockViewModel(string name)
        {
            Name = name;
            RegisterOnPropertyChangedHandler(() => TimeZone, UpdateTimeZone);
            TimeZone = TimeZoneInfo.Local;
        }

        public string Name { get; set; }

        public DateTime Time
        {
            get { return PropertyBag.GetAuto<DateTime>(); }
            set { PropertyBag.SetAuto(value); }
        }

        public string Text
        {
            get { return PropertyBag.GetAuto<string>(); }
            set { PropertyBag.SetAuto(value); }
        }

        public bool IsDay
        {
            get { return PropertyBag.GetAuto<bool>(); }
            set { PropertyBag.SetAuto(value); }
        }

        public TimeZoneInfo TimeZone
        {
            get { return PropertyBag.GetAuto<TimeZoneInfo>(); }
            set { PropertyBag.SetAuto(value); }
        }

        public void Refresh()
        {
            var timeNow = DateTime.UtcNow;
            Time = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZone);
            IsDay = !(timeNow < Sunrise || timeNow > Sunset);
        }

        private void UpdateTimeZone()
        {
            Text = TimeZone.DisplayName;
            Sunrise = TimeZoneInfo.ConvertTimeToUtc(DateTime.Parse("08:00"), TimeZone);
            Sunset = TimeZoneInfo.ConvertTimeToUtc(DateTime.Parse("18:00"), TimeZone);
        }

        private DateTime Sunrise { get; set; }
        private DateTime Sunset { get; set; }
    }
}
