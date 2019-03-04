using System.Collections;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace ModIO
{
    [System.Serializable] [JsonObject]
    public class RequestPage<T>
    {
        // ---------[ FIELDS ]---------
        /// <summary>Maximum number of results returned in this response.</summary>
        [JsonProperty("result_limit")]
        public int size;

        /// <summary>Number of results skipped over.</summary>
        [JsonProperty("result_offset")]
        public int resultOffset;

        /// <summary>Total number of results on the mod.io servers.</summary>
        [JsonProperty("result_total")]
        public int resultTotal;

        /// <summary>The data returned by the request.</summary>
        [JsonProperty("data")]
        public T[] items;
    }
}
