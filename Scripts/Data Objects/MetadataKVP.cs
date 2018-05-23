using Newtonsoft.Json;

namespace ModIO
{
    [System.Serializable]
    public class MetadataKVP
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