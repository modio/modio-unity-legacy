using System.Collections.Generic;

namespace ModIO
{
    /// <summary>Holds a collection of responses for URLs.</summary>
    public static class RequestCache
    {
        // ---------[ Fields ]---------
        /// <summary>Map of url to saved responses.</summary>
        public static Dictionary<string, string> storedResponses
            = new Dictionary<string, string>();

        /// <summary>Fetches a response from the cache.</summary>
        public static bool TryGetResponse(string url, out string response)
        {
            bool success = false;

            success = RequestCache.storedResponses.TryGetValue(url, out response);

            return success;
        }

        /// <summary>Stores a response in the cache.</summary>
        public static void StoreResponse(string url, string responseBody)
        {
            RequestCache.storedResponses[url] = responseBody;
        }
    }
}
