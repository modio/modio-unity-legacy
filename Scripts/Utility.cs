using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ModIO
{
    [Serializable]
    public struct TimeStamp
    {
        // --- CONSTS ---
        private static readonly DateTime UNIX_EPOCH = new DateTime(1970,1,1,0,0,0,0,System.DateTimeKind.Utc);

        // --- CREATION FUNCTIONS ---
        public static TimeStamp GenerateFromLocalDateTime(DateTime localDateTime)
        {
            Debug.Assert(localDateTime.Kind == DateTimeKind.Utc,
                         "Provided DateTime is not Local. Please use TimeStamp.GenerateFromUTCDateTime() instead");

            return GenerateFromUTCDateTime(localDateTime.ToUniversalTime());
        }

        public static TimeStamp GenerateFromUTCDateTime(DateTime utcDateTime)
        {
            Debug.Assert(utcDateTime.Kind == DateTimeKind.Utc,
                         "Provided DateTime is not UTC. Consider using TimeStamp.Now() instead of DateTime.Now() or by converting a local time to UTC using the DateTime.ToUniversalTime() method");

            TimeStamp modDT = new TimeStamp();
            modDT.serverTimeStamp = (int)utcDateTime.Subtract(UNIX_EPOCH).TotalSeconds;
            return modDT;
        }

        public static TimeStamp GenerateFromServerTimeStamp(int timeStamp)
        {
            TimeStamp modDT = new TimeStamp();
            modDT.serverTimeStamp = timeStamp;
            return modDT;
        }

        public static TimeStamp Now()
        {
            return GenerateFromUTCDateTime(DateTime.UtcNow);
        }

        // --- VARIABLES ---
        [SerializeField]
        private int serverTimeStamp;

        // --- ACCESSORS ---
        public DateTime AsLocalDateTime()
        {
            DateTime dateTime = UNIX_EPOCH.AddSeconds(serverTimeStamp).ToLocalTime();
            return dateTime;
        }

        public DateTime AsUTCDateTime()
        {
            DateTime dateTime = UNIX_EPOCH.AddSeconds(serverTimeStamp);
            return dateTime;
        }

        public int AsServerTimeStamp()
        {
            return serverTimeStamp;
        }
    }

    public static class Utility
    {
        // Author: @jon-hanna of StackOverflow (https://stackoverflow.com/users/400547/jon-hanna)
        // URL: https://stackoverflow.com/a/5419544
        public static bool Like(this string toSearch, string toFind)
        {
            return new Regex(@"\A" + new Regex(@"\.|\$|\^|\{|\[|\(|\||\)|\*|\+|\?|\\").Replace(toFind, ch => @"\" + ch).Replace('_', '.').Replace("%", ".*") + @"\z", RegexOptions.Singleline).IsMatch(toSearch);
        }
    }
}