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

        [JsonProperty("data")]
        private T[] _items;

        // ---------[ ACCESSORS ]---------
        /// <summary>
        /// Number of results returned in the current request.
        /// </summary>
        public int Count    { get { return this._count; } }

        /// <summary>
        /// Maximum number of results returned.
        /// </summary>
        /// <remarks>
        /// Defaults to 100 unless overridden by _limit.
        /// </remarks>
        public int Limit    { get { return this._limit; } }


        /// <summary>
        /// Number of results skipped over.
        /// </summary>
        /// <remarks>
        /// Defaults to 0 unless overridden by _offset.
        /// </remarks>
        public int Offset   { get { return this._offset; } }

        /// <summary>
        /// The data returned by the request
        /// </summary>
        public T[] Items    { get { return this._items; } }

        public T this[int index]
        {
            get
            {
                return _items[index];
            }
        }
        // ---------[ ICOLLECTION INTERFACE ]---------
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