using System;
using Debug = UnityEngine.Debug;

namespace ModIO
{
    public static class ServerTimeStamp
    {
        // --- CONSTS ---
        private static readonly DateTime UNIX_EPOCH =
            new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);

        // --- CONVERSION ---
        public static int FromLocalDateTime(DateTime localDateTime)
        {
            Debug.Assert(
                localDateTime.Kind == DateTimeKind.Utc,
                "Provided DateTime is not Local. Please use ServerTimeStamp.FromUTCDateTime() instead");

            return FromUTCDateTime(localDateTime.ToUniversalTime());
        }

        public static int FromUTCDateTime(DateTime utcDateTime)
        {
            Debug.Assert(
                utcDateTime.Kind == DateTimeKind.Utc,
                "Provided DateTime is not UTC. Consider using ServerTimeStamp.Now() instead of DateTime.Now() or by converting a local time to UTC using the DateTime.ToUniversalTime() method");

            int serverTimeStamp = (int)utcDateTime.Subtract(UNIX_EPOCH).TotalSeconds;
            return serverTimeStamp;
        }

        public static DateTime ToLocalDateTime(int serverTimeStamp)
        {
            DateTime dateTime = UNIX_EPOCH.AddSeconds(serverTimeStamp).ToLocalTime();
            return dateTime;
        }

        public static DateTime ToUTCDateTime(int serverTimeStamp)
        {
            DateTime dateTime = UNIX_EPOCH.AddSeconds(serverTimeStamp);
            return dateTime;
        }

        public static int Now
        {
            get {
                return FromUTCDateTime(DateTime.UtcNow);
            }
        }
    }
}
