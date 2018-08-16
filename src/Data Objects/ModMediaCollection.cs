using Newtonsoft.Json;

namespace ModIO
{
    [System.Serializable]
    public class ModMediaCollection
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
        public GalleryImageLocator[] galleryImageLocators;

        // ---------[ ACCESSORS ]---------
        public GalleryImageLocator GetGalleryImageWithFileName(string fileName)
        {
            foreach(var locator in this.galleryImageLocators)
            {
                if(locator.fileName == fileName)
                {
                    return locator;
                }
            }
            return null;
        }
    }
}
