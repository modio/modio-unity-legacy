using Newtonsoft.Json;

namespace ModIO
{
    [System.Serializable]
    [JsonObject]
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

        // ---------[ UTILITY ]------
        /// <summary>Calculates the page count.</summary>
        public int CalculatePageCount()
        {
            if(this.size > 0)
            {
                return UnityEngine.Mathf.CeilToInt((float)this.resultTotal / (float)this.size);
            }
            return -1;
        }

        /// <summary>Calculates the page index.</summary>
        public int CalculatePageIndex()
        {
            if(this.size > 0)
            {
                return this.resultOffset / this.size;
            }
            return -1;
        }
    }
}
