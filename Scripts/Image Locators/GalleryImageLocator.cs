using Newtonsoft.Json;

using Debug = UnityEngine.Debug;

namespace ModIO
{
    [System.Serializable]
    public class GalleryImageLocator : IMultiSizeImageLocator<ModGalleryImageSize>
    {
        // ---------[ FIELDS ]---------
        /// <summary>
        /// Image filename including extension.
        /// </summary>
        [JsonProperty("filename")]
        public string fileName;

        /// <summary>
        /// URL to the full-sized image.
        /// </summary>
        [JsonProperty("original")]
        public string original;

        /// <summary>
        /// URL to the image thumbnail.
        /// </summary>
        [JsonProperty("thumb_320x180")]
        public string thumbnail_320x180;

        // ---------[ INTERFACE IMPLEMENTATION ]---------
        public string GetFileName() { return this.fileName; }
        public string GetURL()      { return this.original; }

        public string GetSizeURL(ModGalleryImageSize size)
        {
            switch(size)
            {
                case ModGalleryImageSize.Original:
                {
                    return this.original;
                }
                case ModGalleryImageSize.Thumbnail_320x180:
                {
                    return this.thumbnail_320x180;
                }
                default:
                {
                    Debug.LogError("[mod.io] Unrecognized ModGalleryImageSize");
                    return string.Empty;
                }
            }
        }
    }
}
