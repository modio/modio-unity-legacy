using Newtonsoft.Json;

namespace ModIO
{
    [System.Serializable]
    public class HeaderImageLocator : IImageLocator
    {
        // ---------[ FIELDS ]---------
        /// <summary>
        /// Header image filename including extension.
        /// </summary>
        [JsonProperty("filename")]
        public string fileName;

        /// <summary>
        /// URL to the full-sized header image.
        /// </summary>
        [JsonProperty("original")]
        public string url;

        // ---------[ INTERFACE IMPLEMENTATION ]---------
        public string GetFileName()
        {
            return this.fileName;
        }
        public string GetURL()
        {
            return this.url;
        }
    }
}
