using Newtonsoft.Json;

namespace ModIO
{
    [System.Serializable]
    public class ModfileLocator
    {
        // ---------[ FIELDS ]---------
        /// <summary>URL to download the file from the mod.io CDN.</summary>
        /// <remarks>
        /// If the game requires mod downloads to be initiated via the API,
        /// the binaryURL returned will contain a verification hash.
        /// This hash must be supplied to get the modfile, and will
        /// expire after a certain period of time. Saving and reusing the
        /// binaryURL won't work in this situation given its dynamic nature.
        /// </remarks>
        [JsonProperty("binary_url")]
        public string binaryURL;

        /// <summary>Unix timestamp of when the binary_url will expire.</summary>
        [JsonProperty("date_expires")]
        public int dateExpires;
    }
}
