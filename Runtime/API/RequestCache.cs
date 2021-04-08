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

        /// <summary>Max cache size.</summary>
        private const uint MAX_CACHE_SIZE = 1024*1024;

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
            int entryIndex;

            if(LocalUser.OAuthToken == RequestCache.lastOAuthToken
               && RequestCache.TryGetEntry(url, out entryIndex, out entry))
            {
                // check if stale
                if((ServerTimeStamp.Now - entry.timeStamp) <= RequestCache.ENTRY_LIFETIME)
                {
                    // clear it, and any entries older than it
                    RequestCache.RemoveOldestEntries(entryIndex + 1);
                }
                else
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

            // remove stale entry
            int oldIndex;
            Entry oldValue;
            if(RequestCache.TryGetEntry(url, out oldIndex, out oldValue))
            {
                Debug.LogWarning("[mod.io] Stale cached request found. Removing all older entries.");
                RequestCache.RemoveOldestEntries(oldIndex + 1);
            }

            // calculate new entry size
            uint size = 0;
            if(responseBody != null)
            {
                size = (uint)responseBody.Length * sizeof(char);
            }

            // trim cache if necessary
            if(size > RequestCache.MAX_CACHE_SIZE)
            {
                Debug.Log("[mod.io] Could not cache entry as the response body is larger than MAX_CACHE_SIZE."
                          + "\nURL=" + url);
                return;
            }

            if(RequestCache.currentCacheSize + size > RequestCache.MAX_CACHE_SIZE)
            {
                RequestCache.TrimCacheToMaxSize(RequestCache.MAX_CACHE_SIZE - size);
            }

            // add new entry
            Entry newValue = new Entry()
            {
                timeStamp = ServerTimeStamp.Now,
                responseBody = responseBody,
                size = size,
            };

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

        /// <summary>Removes entries from the cache until the total cache size is below the given value.</summary>
        private static void TrimCacheToMaxSize(uint maxSize)
        {
            uint trimmedSize = RequestCache.currentCacheSize;
            int lastIndex;

            for(lastIndex = 0;
                lastIndex < RequestCache.responses.Count && trimmedSize > maxSize;
                ++lastIndex)
            {
                trimmedSize -= RequestCache.responses[lastIndex].size;
            }

            if(trimmedSize > 0)
            {
                RequestCache.RemoveOldestEntries(lastIndex + 1);
            }
            else
            {
                RequestCache.Clear();
            }
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

            // update url map
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

            // update cache size
            uint sizeToRemove = 0;
            for(int i = 0; i < count; ++i)
            {
                sizeToRemove += RequestCache.responses[i].size;
            }
            RequestCache.currentCacheSize -= sizeToRemove;

            // remove responses
            RequestCache.responses.RemoveRange(0, count);
        }
    }
}
