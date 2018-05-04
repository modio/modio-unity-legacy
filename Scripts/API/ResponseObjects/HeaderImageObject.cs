using Newtonsoft.Json;

namespace ModIO.API
{
    [System.Serializable]
    public struct HeaderImageObject
    {
        /// <summary>Header image filename including extension.</summary>
        [JsonProperty("filename")]
        public string fileName;

        /// <summary>URL to the full-sized header image.</summary>
        [JsonProperty("original")]
        public string fullSize;
    }
}