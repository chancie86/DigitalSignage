using System.IO;

namespace Display.ViewModels
{
    internal static class WebHelpers
    {
        #region Google account 'citrixchalfont'
        internal static string GoogleWebApiKey = "AIzaSyCrCtIIRWjwfNPfdzCPPUn98hnOLxQGU_Y";
        internal static string GoogleMapsCitrixChalfontOfficePlaceId = "ChIJ2ZV7d7NodkgRAV1KM8cegR4";
        #endregion

        #region OpenWeatherMap account 'citrixchalfont@gmail.com'
        internal static string OpenWeatherMapApiKey = "bc6685de21372714c263998cec6be183";
        #endregion

        #region Twitter
        internal static string TwitterConsumerKey = "utf71G9JNpoS1b2UwlLg9aysq";
        internal static string TwitterConsumerSecret = "AnKIr2RvaJVXP4ZUVPC7nwevm3K9yEMI88vtS9tNiVXPnlOfkV";
        #endregion

        public static string ApplicationTempDirectory
        {
            get
            {
                return Path.Combine(Path.GetTempPath(), "Display");
            }
        }

        public static string TwitterProfileImagePath
        {
            get
            {
                return Path.Combine(ApplicationTempDirectory, "Twitter", "Profile");
            }
        }
    }
}
