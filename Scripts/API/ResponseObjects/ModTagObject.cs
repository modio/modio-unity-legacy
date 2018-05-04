using Newtonsoft.Json;

namespace ModIO.API
{
    [System.Serializable]
    public struct ModTagObject
    {
        /// <summary>Tag name.</summary>
        [JsonProperty("name")]
        public string name;

        /// <summary>Unix timestamp of date tag was applied.</summary>
        [JsonProperty("date_added")]
        public int dateAdded;
    }
}