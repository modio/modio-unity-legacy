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

        // ---------[ Access Interface ]---------
        /// <summary>Fetches a response from the cache.</summary>
        public static bool TryGetResponse(string url, out string response)
        {
            response = null;
            string endpointURL = null;

            // try to remove the apiURL
            if(!RequestCache.TryTrimAPIURLAndKey(url, out endpointURL)) { return false; }

            bool success = false;
            Entry entry;
            int entryIndex;

            if(LocalUser.OAuthToken == RequestCache.lastOAuthToken
               && RequestCache.TryGetEntry(endpointURL, out entryIndex, out entry))
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

            string endpointURL = null;

            // try to remove the apiURL
            if(!RequestCache.TryTrimAPIURLAndKey(url, out endpointURL))
            {
                Debug.LogWarning("[mod.io] Attempted to cache response for url that does not contain the api URL."
                                 + "\nRequest URL: " + (url == null ? "NULL" : url));
                return;
            }

            // remove stale entry
            int oldIndex;
            Entry oldValue;
            if(RequestCache.TryGetEntry(endpointURL, out oldIndex, out oldValue))
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
                          + "\nMAX_CACHE_SIZE=" + ValueFormatting.ByteCount(RequestCache.MAX_CACHE_SIZE, "0.0")
                          + "\nendpointURL=" + endpointURL
                          + "\nResponseBody Size=" + ValueFormatting.ByteCount(size, "0.0"));
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

            RequestCache.urlResponseIndexMap.Add(endpointURL, RequestCache.responses.Count);
            RequestCache.responses.Add(newValue);

            RequestCache.currentCacheSize += size;
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

        // ---------[ Utility ]---------
        /// <summary>Gets the index and entry for a URL.</summary>
        private static bool TryGetEntry(string url, out int index, out Entry entry)
        {
            string endpointURL = null;
            if(!RequestCache.TryTrimAPIURLAndKey(url, out endpointURL)
               || !RequestCache.urlResponseIndexMap.TryGetValue(endpointURL, out index)
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

        /// <summary>Trims the API URL and API Key from the request URL.</summary>
        private static bool TryTrimAPIURLAndKey(string requestURL, out string endpointURL)
        {
            if(string.IsNullOrEmpty(requestURL)
               || !requestURL.StartsWith(PluginSettings.API_URL)
               || requestURL.Length == PluginSettings.API_URL.Length)
            {
                endpointURL = null;
                return false;
            }

            endpointURL = requestURL.Substring(PluginSettings.API_URL.Length+1).Replace("&api_key=" + PluginSettings.GAME_API_KEY, string.Empty);
            return true;
        }

        #if DEBUG

        /// <summary>Generates the string for debugging the data in the request cache.</summary>
        public static string GenerateDebugInfo(int responseBodyCharacterLimit)
        {
            var s = new System.Text.StringBuilder();

            s.AppendLine("currentCacheSize="
                         + ValueFormatting.ByteCount(RequestCache.currentCacheSize, "0.0")
                         + "/"
                         + ValueFormatting.ByteCount(RequestCache.MAX_CACHE_SIZE, "0.0"));

            s.AppendLine("urlResponseIndexMap=");
            foreach(var kvp in RequestCache.urlResponseIndexMap)
            {
                s.AppendLine("[" + kvp.Value.ToString("00") + "]:" + kvp.Key);
            }

            s.AppendLine("responses=");
            for(int i = 0; i < RequestCache.responses.Count; ++i)
            {
                Entry e = RequestCache.responses[i];
                s.Append("[" + i.ToString("00") + "]:");
                s.Append(ServerTimeStamp.ToLocalDateTime(e.timeStamp).ToString());
                s.Append("--");
                s.AppendLine(ValueFormatting.ByteCount(e.size, "0.00"));

                string r = e.responseBody;
                if(string.IsNullOrEmpty(r))
                {
                    r = "[NULL-OR-EMPTY]";
                }
                else
                {
                    r = r.Substring(0, UnityEngine.Mathf.Min(responseBodyCharacterLimit, r.Length));
                }
                s.AppendLine(">>" + r);
            }

            return s.ToString();
        }


        #endif // DEBUG
    }
}
