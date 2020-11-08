using System.Runtime.Serialization;

namespace Display.ViewModels.Weather
{
    [DataContract]
    internal class WeatherResponse
    {
        [DataMember(Name = "city")]
        public City City { get; set; }

        [DataMember(Name = "list")]
        public List[] List { get; set; }
    }

    [DataContract]
    internal class City
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }
    }

    [DataContract]
    internal class List
    {
        [DataMember(Name = "dt")]
        public long Dt { get; set; }

        [DataMember(Name = "temp")]
        public Temp Temp { get; set; }

        [DataMember(Name = "weather")]
        public Weather[] Weather { get; set; }
    }

    [DataContract]
    internal class Temp
    {
        [DataMember(Name = "min")]
        public double Min { get; set; }

        [DataMember(Name = "max")]
        public double Max { get; set; }
    }

    [DataContract]
    internal class Weather
    {
        private WeatherIcon _icon = WeatherIcon.Unknown;

        [DataMember(Name = "main")]
        public string Main { get; set; }

        [DataMember(Name = "description")]
        public string Description { get; set; }

        [DataMember(Name = "icon")]
        public string IconId { get; set; }

        /// <summary>
        /// http://openweathermap.org/weather-conditions
        /// </summary>
        public WeatherIcon Icon
        {
            get
            {
                if (_icon != WeatherIcon.Unknown)
                    return _icon;

                _icon = WeatherIconHelper.ConvertIdToWeatherIcon(IconId);

                return _icon;
            }
        }

        public bool Day
        {
            get { return IconId.EndsWith("d"); }
        }
    }
}
