using Newtonsoft.Json;

namespace ModIO.API
{
    [System.Serializable]
    public struct HeaderImageObject : IImageLocator2
    {
        // ---------[ FIELDS ]---------
        /// <summary>Header image filename including extension.</summary>
        [JsonProperty("filename")]
        public string fileName;

        /// <summary>URL to the full-sized header image.</summary>
        [JsonProperty("original")]
        public string url;

        // ---------[ INTERFACE IMPLEMENTATION ]---------
        public string GetFileName() { return this.fileName; }
        public string GetURL()      { return this.url; }
    }
}