using System;

namespace Display
{
    public static class TimeHelpers
    {
        private static readonly TimeZoneInfo DefaultTimeZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");

        public static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static DateTime TimeFromEpochTimestamp(long timestamp, TimeZoneInfo targetTimeZone = null)
        {
            if (targetTimeZone == null)
                targetTimeZone = DefaultTimeZone;

            return TimeZoneInfo.ConvertTimeFromUtc(Epoch.AddMilliseconds(timestamp), targetTimeZone);
        }
    }
}
