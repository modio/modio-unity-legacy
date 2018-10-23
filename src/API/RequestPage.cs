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

namespace ModIO.API
{
    [System.Obsolete("Use ModIO.RequestPage instead.")]
    public class ResponseArray<T> : RequestPage<T>, IEnumerable<T>
    {
        // ---------[ ACCESSORS ]---------
        public int Limit        { get { return this.size; } }
        public int Offset       { get { return this.resultOffset; } }
        public int Total        { get { return this.resultTotal; } }
        public int Count        { get { return this.items.Length; } }
        public T[] Items        { get { return this.items; } }

        public T this[int index]
        {
            get
            {
                return this.items[index];
            }
        }

        // ---------[ IENUMERABLE INTERFACE ]---------
        public IEnumerator<T> GetEnumerator()
        {
            foreach(T o in this.items)
            {
                yield return o;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
