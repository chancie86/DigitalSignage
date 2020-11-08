using System.Collections.Generic;

namespace Display.ViewModels.Weather
{
    public static class WeatherIconHelper
    {
        private static readonly Dictionary<string, WeatherIcon> IdIconMap = new Dictionary<string, WeatherIcon>
        {
            {"01", WeatherIcon.Clear},
            {"02", WeatherIcon.FewClouds},
            {"03", WeatherIcon.ScatteredClouds},
            {"04", WeatherIcon.BrokenClouds},
            {"09", WeatherIcon.Showers},
            {"10", WeatherIcon.Rain},
            {"11", WeatherIcon.Thunder},
            {"13", WeatherIcon.Snow},
            {"50", WeatherIcon.Mist},
        };

        private static readonly Dictionary<WeatherIcon, string> IconIdMap;

        static WeatherIconHelper()
        {
            // Create the reverse of idIconMap
            IconIdMap = new Dictionary<WeatherIcon, string>();
            foreach (var kvp in IdIconMap)
            {
                IconIdMap[kvp.Value] = kvp.Key;
            }
        }

        public static WeatherIcon ConvertIdToWeatherIcon(string id)
        {
            var code = id.Substring(0, 2);

            WeatherIcon result;
            return IdIconMap.TryGetValue(code, out result) ? result : WeatherIcon.Unknown;
        }

        public static string ConvertWeatherIconToId(WeatherIcon icon)
        {
            string result;
            return IconIdMap.TryGetValue(icon, out result) ? result : null;
        }

        public static string GetDayFileName(WeatherIcon icon)
        {
            return ConvertWeatherIconToId(icon) + "d.png";
        }

        public static string GetNightFileName(WeatherIcon icon)
        {
            return ConvertWeatherIconToId(icon) + "n.png";
        }
    }
}
