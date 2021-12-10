using System;
namespace Rosi.Runtime.Core
{
    public static class Time
    {
        public static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public static long Timestamp => DateTimeToTimestamp(DateTime.UtcNow);

        public static long DateTimeToTimestamp(DateTime value)
        {
            var elapsedTime = value - Epoch;
            return (long)elapsedTime.TotalMilliseconds;
        }

        public static DateTime TimestampToDateTime(long timestamp)
        {
            return Epoch.AddMilliseconds(timestamp);
        }

        public static string DateTimeString(long timestamp, bool localTime)
        {
            var date = TimestampToDateTime(timestamp);
            if (localTime)
                date.ToLocalTime();

            return $"{ date.ToShortDateString() } { date.ToShortTimeString() }";
        }

        public static string TimeString(long timestamp, bool localTime)
        {
            var date = TimestampToDateTime(timestamp);
            if (localTime)
                date.ToLocalTime();

            return date.ToShortTimeString();
        }

        public static string DateString(long timestamp, bool localTime)
        {
            var date = TimestampToDateTime(timestamp);
            if (localTime)
                date.ToLocalTime();

            return date.ToShortDateString();
        }
    }
}
