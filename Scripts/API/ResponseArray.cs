using System.Collections;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace ModIO.API
{
    [System.Serializable] [JsonObject]
    public class ResponseArray<T> : IEnumerable<T>
    {
        // ---------[ FIELDS ]---------
        [JsonProperty("result_count")]
        private int _count;

        [JsonProperty("result_limit")]
        private int _limit;

        [JsonProperty("result_offset")]
        private int _offset;

        [JsonProperty("result_total")]
        private int _total;

        [JsonProperty("data")]
        private T[] _items;

        // ---------[ ACCESSORS ]---------
        /// <summary>Number of items in this response.</summary>
        public int Count    { get { return this._count; } }

        /// <summary>Maximum number of results returned in this response.</summary>
        public int Limit    { get { return this._limit; } }

        /// <summary>Number of results skipped over.</summary>
        public int Offset   { get { return this._offset; } }

        /// <summary>Total number of results on the mod.io servers.</summary>
        public int Total    { get { return this._total; } }

        /// <summary>The data returned by the request.</summary>
        public T[] Items    { get { return this._items; } }

        public T this[int index]
        {
            get
            {
                return _items[index];
            }
        }

        // ---------[ IENUMERABLE INTERFACE ]---------
        public IEnumerator<T> GetEnumerator()
        {
            foreach(T o in _items)
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
