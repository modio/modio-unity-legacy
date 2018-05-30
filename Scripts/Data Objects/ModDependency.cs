using Newtonsoft.Json;

namespace ModIO
{
    [System.Serializable]
    public class ModDependency
    {
        // ---------[ FIELDS ]---------
        /// <summary>
        /// Unique id of the mod that is the dependency.
        /// </summary>
        [JsonProperty("mod_id")]
        public int modId;

        /// <summary>
        /// Unix timestamp of date the dependency was added.
        /// </summary>
        [JsonProperty("date_added")]
        public int dateAdded;
    }
}
