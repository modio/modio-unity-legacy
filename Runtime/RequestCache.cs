using System.Collections.Generic;

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
            Entry entry = new Entry()
            {
                timeStamp = ServerTimeStamp.Now,
                responseBody = responseBody,
            };

            RequestCache.storedResponses[url] = entry;
        }
    }
}
