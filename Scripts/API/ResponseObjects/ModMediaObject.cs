using Newtonsoft.Json;

namespace ModIO.API
{
    [System.Serializable]
    public struct ModMediaObject
    {
        // ---------[ FIELDS ]---------
        /// <summary>Array of YouTube links.</summary>
        [JsonProperty("youtube")]
        public string[] youtubeURLs;

        /// <summary>Array of SketchFab links.</summary>
        [JsonProperty("sketchfab")]
        public string[] sketchfabURLs;

        /// <summary>Array of image objects (a gallery).</summary>
        [JsonProperty("images")]
        public ImageObject[] galleryImages;
    }
}
