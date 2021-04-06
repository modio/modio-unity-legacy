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
        private static Dictionary<string, Entry> urlResponseMap
            = new Dictionary<string, Entry>();

        /// <summary>Map of url to saved responses.</summary>
        private static Dictionary<string, int> urlResponseIndexMap = new Dictionary<string, int>();

        /// <summary>List of saved responses.</summary>
        private static List<Entry> responses = new List<Entry>();

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
                success = RequestCache.urlResponseMap.TryGetValue(url, out entry);
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
            if(RequestCache.urlResponseMap.TryGetValue(url, out oldValue))
            {
                RequestCache.currentCacheSize -= oldValue.size;

                RequestCache.urlResponseMap[url] = newValue;
                RequestCache.currentCacheSize += newValue.size;
            }
            else
            {
                RequestCache.urlResponseMap.Add(url, newValue);
                RequestCache.currentCacheSize += newValue.size;
            }

            // add to stores
            RequestCache.urlResponseIndexMap.Add(url, RequestCache.responses.Count);
            RequestCache.responses.Add(newValue);
        }

        /// <summary>Clears the data from the cache.</summary>
        public static void Clear()
        {
            RequestCache.urlResponseMap.Clear();
            RequestCache.urlResponseIndexMap.Clear();
            RequestCache.responses.Clear();
            RequestCache.currentCacheSize = 0;
        }

        /// <summary>Gets the index and entry for a URL.</summary>
        private static bool TryGetEntry(string url, out int index, out Entry entry)
        {
            if(string.IsNullOrEmpty(url)
               || !RequestCache.urlResponseIndexMap.TryGetValue(url, out index)
               || index < 0
               || index >= RequestCache.responses.Count)
            {
                index = -1;
                entry = new Entry();
                return false;
            }
            else
            {
                entry = RequestCache.responses[index];
                return true;
            }
        }

        /// <summary>Removes the oldest entries from the cache.</summary>
        private static void RemoveOldestEntries(int count)
        {
            Debug.Assert(count > 0);

            // check if clearing all
            if(count >= RequestCache.responses.Count)
            {
                RequestCache.Clear();
                return;
            }

            RequestCache.responses.RemoveRange(0, count);

            List<string> urlKeys = new List<string>(RequestCache.urlResponseIndexMap.Keys);
            foreach(string url in urlKeys)
            {
                int newValue = RequestCache.urlResponseIndexMap[url] - count;
                RequestCache.urlResponseIndexMap[url] = newValue;

                if(newValue < 0)
                {
                    RequestCache.urlResponseIndexMap.Remove(url);
                }
            }
        }
    }
}
