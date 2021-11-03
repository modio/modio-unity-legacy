using Newtonsoft.Json;

namespace ModIO
{
    [System.Serializable]
    public class ModTag
    {
        // ---------[ FIELDS ]---------
        /// <summary>Tag name.</summary>
        [JsonProperty("name")]
        public string name;

        /// <summary>Unix timestamp of date tag was applied.</summary>
        [JsonProperty("date_added")]
        public int dateAdded;
    }
}
