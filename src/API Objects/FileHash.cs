using Newtonsoft.Json;

namespace ModIO
{
    [System.Serializable]
    public class FileHash
    {
        // ---------[ FIELDS ]---------
        /// <summary>MD5 hash of a file.</summary>
        [JsonProperty("md5")]
        public string md5;
    }
}
