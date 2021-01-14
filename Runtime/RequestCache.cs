using System.Collections.Generic;

using Debug = UnityEngine.Debug;

namespace ModIO
{
    /// <summary>Holds a collection of responses for URLs.</summary>
    public static class RequestCache
    {
        // ---------[ Nested Data-Types ]---------
        /// <summary>Entry for a stored response in the request cache.</summary>
        public struct Entry
        {
            public int timeStamp;
            public string responseBody;
        }

        // ---------[ Constants ]---------
        /// <summary>Number of seconds for which a cached response is considered valid.</summary>
        public const int ENTRY_LIFETIME = 15;

        // ---------[ Fields ]---------
        /// <summary>Map of url to saved responses.</summary>
        public static Dictionary<string, Entry> storedResponses
            = new Dictionary<string, Entry>();

        /// <summary>Fetches a response from the cache.</summary>
        public static bool TryGetResponse(string url, out string response)
        {
            bool success = false;

            Entry entry;
            success = RequestCache.storedResponses.TryGetValue(url, out entry);
            success &= (ServerTimeStamp.Now - entry.timeStamp) <= RequestCache.ENTRY_LIFETIME;

            if(success)
            {
                response = entry.responseBody;
            }
            else
            {
                response = null;
            }

            return success;
        }

        /// <summary>Stores a response in the cache.</summary>
        public static void StoreResponse(string url, string responseBody)
        {
            if(string.IsNullOrEmpty(url))
            {
                Debug.LogWarning("[mod.io] Attempted to cache response for null or empty URL.");
                return;
            }

            Entry entry = new Entry()
            {
                timeStamp = ServerTimeStamp.Now,
                responseBody = responseBody,
            };

            RequestCache.storedResponses[url] = entry;
        }
    }
}
