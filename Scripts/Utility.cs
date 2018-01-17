using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ModIO
{
    // TODO(@jackson): Implement IComparable
    [Serializable]
    public struct ModIOTimestamp
    {
        // --- CONSTS ---
        private static readonly DateTime UNIX_EPOCH = new DateTime(1970,1,1,0,0,0,0,System.DateTimeKind.Utc);

        // --- CREATION FUNCTIONS ---
        public static ModIOTimestamp GenerateFromLocalDateTime(DateTime localDateTime)
        {
            Debug.Assert(localDateTime.Kind == DateTimeKind.Utc,
                         "Provided DateTime is not Local. Please use ModIOTimestamp.GenerateFromUTCDateTime() instead");

            return GenerateFromUTCDateTime(localDateTime.ToUniversalTime());
        }

        public static ModIOTimestamp GenerateFromUTCDateTime(DateTime utcDateTime)
        {
            Debug.Assert(utcDateTime.Kind == DateTimeKind.Utc,
                         "Provided DateTime is not UTC. Consider using ModIOTimestamp.Now() instead of DateTime.Now() or by converting a local time to UTC using the DateTime.ToUniversalTime() method");

            ModIOTimestamp modDT = new ModIOTimestamp();
            modDT.serverTimestamp = (int)utcDateTime.Subtract(UNIX_EPOCH).TotalSeconds;
            return modDT;
        }

        public static ModIOTimestamp GenerateFromServerTimestamp(int timestamp)
        {
            ModIOTimestamp modDT = new ModIOTimestamp();
            modDT.serverTimestamp = timestamp;
            return modDT;
        }

        public static ModIOTimestamp Now()
        {
            return GenerateFromUTCDateTime(DateTime.UtcNow);
        }

        // --- VARIABLES ---
        [SerializeField]
        private int serverTimestamp;

        // --- ACCESSORS ---
        public DateTime AsLocalDateTime()
        {
            DateTime dateTime = UNIX_EPOCH.AddSeconds(serverTimestamp).ToLocalTime();
            return dateTime;
        }

        public DateTime AsUTCDateTime()
        {
            DateTime dateTime = UNIX_EPOCH.AddSeconds(serverTimestamp);
            return dateTime;
        }

        public int AsServerTimestamp()
        {
            return serverTimestamp;
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