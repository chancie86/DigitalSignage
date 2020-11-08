using System;
using WpfUtils;

namespace Display.ViewModels.Weather
{
    public class DailyWeather
        : BaseViewModel
    {
        public DateTime Date
        {
            get { return PropertyBag.GetAuto<DateTime>(); }
            set { PropertyBag.SetAuto(value); }
        }

        public int MinTemp
        {
            get { return PropertyBag.GetAuto<int>(); }
            set { PropertyBag.SetAuto(value); }
        }

        public int MaxTemp
        {
            get { return PropertyBag.GetAuto<int>(); }
            set { PropertyBag.SetAuto(value); }
        }

        public WeatherIcon Icon
        {
            get { return PropertyBag.GetAuto<WeatherIcon>(); }
            set { PropertyBag.SetAuto(value); }
        }

        public string ShortDescription
        {
            get { return PropertyBag.GetAuto<string>(); }
            set { PropertyBag.SetAuto(value); }
        }

        public string LongDescription
        {
            get { return PropertyBag.GetAuto<string>(); }
            set { PropertyBag.SetAuto(value); }
        }

        public string IconPath
        {
            get { return PropertyBag.GetAuto<string>(); }
            set { PropertyBag.SetAuto(value); }
        }
    }
}
