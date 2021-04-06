using System.Collections.Generic;

using Debug = UnityEngine.Debug;

namespace ModIO.API
{
    /// <summary>Holds a collection of responses for URLs.</summary>
    public static class RequestCache
    {
        // ---------[ Nested Data-Types ]---------
        /// <summary>Entry for a stored response in the request cache.</summary>
        private struct Entry
        {
            public int timeStamp;
            public string responseBody;
            public uint size;
        }

        // ---------[ Constants ]---------
        /// <summary>Number of seconds for which a cached response is considered valid.</summary>
        private const int ENTRY_LIFETIME = 15;

        // ---------[ Fields ]---------
        /// <summary>Map of url to saved responses.</summary>
        private static Dictionary<string, Entry> storedResponses
            = new Dictionary<string, Entry>();

        /// <summary>OAuthToken present during the last StoreResponse call.</summary>
        private static string lastOAuthToken = null;

        /// <summary>Current running size of the cache.</summary>
        private static uint currentCacheSize = 0;

        /// <summary>Fetches a response from the cache.</summary>
        public static bool TryGetResponse(string url, out string response)
        {
            response = null;

            // early out for null URL
            if(string.IsNullOrEmpty(url)) { return false; }

            bool success = false;
            Entry entry;

            if(LocalUser.OAuthToken == RequestCache.lastOAuthToken)
            {
                success = RequestCache.storedResponses.TryGetValue(url, out entry);
                success &= (ServerTimeStamp.Now - entry.timeStamp) <= RequestCache.ENTRY_LIFETIME;

                if(success)
                {
                    response = entry.responseBody;
                }
            }

            return success;
        }

        /// <summary>Stores a response in the cache.</summary>
        public static void StoreResponse(string url, string responseBody)
        {
            if(LocalUser.OAuthToken != RequestCache.lastOAuthToken)
            {
                RequestCache.Clear();
                RequestCache.lastOAuthToken = LocalUser.OAuthToken;
            }

            if(string.IsNullOrEmpty(url))
            {
                Debug.LogWarning("[mod.io] Attempted to cache response for null or empty URL.");
                return;
            }

            // build new entry values
            uint size = 0;
            if(responseBody != null)
            {
                size = (uint)responseBody.Length * sizeof(char);
            }

            Entry newValue = new Entry()
            {
                timeStamp = ServerTimeStamp.Now,
                responseBody = responseBody,
                size = size,
            };

            // handle entry adding / replacement
            Entry oldValue;
            if(RequestCache.storedResponses.TryGetValue(url, out oldValue))
            {
                RequestCache.currentCacheSize -= oldValue.size;

                RequestCache.storedResponses[url] = newValue;
                RequestCache.currentCacheSize += newValue.size;
            }
            else
            {
                RequestCache.storedResponses.Add(url, newValue);
                RequestCache.currentCacheSize += newValue.size;
            }
        }

        /// <summary>Removes an entry from the cache.</summary>
        public static void RemoveResponse(string url)
        {
            if(string.IsNullOrEmpty(url)) { return; }

            Entry entry;
            if(RequestCache.storedResponses.TryGetValue(url, out entry))
            {
                RequestCache.currentCacheSize -= entry.size;
                RequestCache.storedResponses.Remove(url);
            }
        }

        /// <summary>Clears the data from the cache.</summary>
        public static void Clear()
        {
            RequestCache.storedResponses.Clear();
            RequestCache.currentCacheSize = 0;
        }
    }
}
