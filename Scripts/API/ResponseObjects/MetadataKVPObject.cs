using Newtonsoft.Json;

namespace ModIO.API
{
    [System.Serializable]
    public struct MetadataKVPObject
    {
        // ---------[ FIELDS ]---------
        /// <summary>The key of the key-value pair.</summary>
        [JsonProperty("metakey")]
        public string key;

        /// <summary>The value of the key-value pair.</summary>
        [JsonProperty("metavalue")]
        public string value;
    }
}