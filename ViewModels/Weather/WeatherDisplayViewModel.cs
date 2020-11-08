using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;

namespace Display.ViewModels.Weather
{
    public class WeatherDisplayViewModel
        : DisplayBaseViewModel
    {
        private const string Url = "http://api.openweathermap.org/data/2.5/forecast";
        private const string IconUrl = "http://openweathermap.org/img/w/{0}";

        private readonly RestClient _restClient;

        private string _location;
        private string _directory;

        public WeatherDisplayViewModel(string location)
        {
            DailyWeathers = new ObservableCollection<DailyWeather>();

            UpdateLocation(location);

            _restClient = new RestClient();

            Refresh += OnRefresh;
        }

        public Units Units
        {
            get { return PropertyBag.GetAuto<Units>(); }
            set { PropertyBag.SetAuto(value); }
        }

        public string BackgroundImagePath
        {
            get { return PropertyBag.GetAuto<string>(); }
            set { PropertyBag.SetAuto(value); }
        }

        public ObservableCollection<DailyWeather> DailyWeathers { get; private set; }

        protected override int RefreshIntervalInMinutes { get { return 60*12; } }

        public void UpdateLocation(string location)
        {
            if (string.IsNullOrEmpty(location))
                throw new ArgumentNullException();

            Title = location;
            _location = location.Replace(" ", "+");
        }

        private void OnRefresh()
        {
            if (!Initialized)
                Initialize();

            var restArgs = new Dictionary<string, string>
            {
                {"daily", "location"},
                {"q", _location},
                {"mode", "json"},
                {"cnt", "10"},
                {"units", Units == Units.Metric ? "metric" : "imperial"},
                {"appid", WebHelpers.OpenWeatherMapApiKey}
            };

            var response = _restClient.MakeRequest<WeatherResponse>(Url, "daily", null, restArgs);

            Title = response.City.Name;

            Dispatcher.Invoke(() =>
            {
                BackgroundImagePath = null;

                var newDailyWeathers = new Collection<DailyWeather>();

                for (var i = 0; i < response.List.Length; i++)
                {
                    var list = response.List[i];

                    var dailyWeather = new DailyWeather
                    {
                        Date = TimeHelpers.TimeFromEpochTimestamp(list.Dt*1000),
                        MaxTemp = (int)Math.Ceiling(list.Temp.Max),
                        MinTemp = (int)Math.Floor(list.Temp.Min)
                    };

                    var weather = list.Weather.FirstOrDefault();
                    if (weather != null)
                    {
                        dailyWeather.Icon = weather.Icon;
                        dailyWeather.ShortDescription = weather.Main;
                        dailyWeather.LongDescription = weather.Description;
                        dailyWeather.IconPath = Path.Combine(_directory, weather.Day
                            ? WeatherIconHelper.GetDayFileName(weather.Icon)
                            : WeatherIconHelper.GetNightFileName(weather.Icon));

                        if (newDailyWeathers.Count == 0)
                        {
                            switch (weather.Icon)
                            {
                                case WeatherIcon.Clear:
                                    BackgroundImagePath = @"..\..\Images\weather_clear.jpg";
                                    break;
                                case WeatherIcon.FewClouds:
                                    BackgroundImagePath = @"..\..\Images\weather_clouds.jpg";
                                    break;
                                case WeatherIcon.ScatteredClouds:
                                case WeatherIcon.BrokenClouds:
                                    BackgroundImagePath = @"..\..\Images\weather_overcast.jpg";
                                    break;
                                case WeatherIcon.Showers:
                                case WeatherIcon.Rain:
                                    BackgroundImagePath = @"..\..\Images\weather_rain.jpg";
                                    break;
                                case WeatherIcon.Thunder:
                                    BackgroundImagePath = @"..\..\Images\weather_thunder.jpg";
                                    break;
                                case WeatherIcon.Snow:
                                    BackgroundImagePath = @"..\..\Images\weather_snow.jpg";
                                    break;
                                case WeatherIcon.Mist:
                                    BackgroundImagePath = @"..\..\Images\weather_fog.jpg";
                                    break;
                                default:
                                    BackgroundImagePath = null;
                                    break;
                            }
                        }
                    }

                    newDailyWeathers.Add(dailyWeather);
                }

                DailyWeathers.Clear();
                DailyWeathers.AddRange(newDailyWeathers);
            });
        }

        private void Initialize()
        {
            Log.TraceEntry();

            // Download the weather icons

            _directory = Path.Combine(WebHelpers.ApplicationTempDirectory, "Weather", Environment.UserName);

            if (!Directory.Exists(_directory))
                Directory.CreateDirectory(_directory);

            using (var client = new WebClient())
            {
                var icons = Enum.GetValues(typeof (WeatherIcon)).Cast<WeatherIcon>();
                
                foreach (var icon in icons)
                {
                    if (icon == WeatherIcon.Unknown)
                        continue;

                    var fileName = WeatherIconHelper.GetDayFileName(icon);
                    var url = string.Format(IconUrl, fileName);
                    client.DownloadFile(url, Path.Combine(_directory, fileName));

                    fileName = WeatherIconHelper.GetNightFileName(icon);
                    url = string.Format(IconUrl, fileName);
                    client.DownloadFile(url, Path.Combine(_directory, fileName));
                }
            }

            Log.TraceExit();
        }
    }
}
