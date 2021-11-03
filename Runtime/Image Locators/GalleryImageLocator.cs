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
        /// <summary>Returns the FileName for the image.</summary>
        public string GetFileName()
        {
            return this.fileName;
        }
        /// <summary>Returns the URL for the original image.</summary>
        public string GetURL()
        {
            return this.original;
        }
        /// <summary>Returns the URL for the given size.</summary>
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
        /// <summary>Returns all the URLs in the image locator.</summary>
        public SizeURLPair<ModGalleryImageSize>[] GetAllURLs()
        {
            return new SizeURLPair<ModGalleryImageSize>[] {
                new SizeURLPair<ModGalleryImageSize>() {
                    size = ModGalleryImageSize.Original,
                    url = this.original,
                },
                new SizeURLPair<ModGalleryImageSize>() {
                    size = ModGalleryImageSize.Thumbnail_320x180,
                    url = this.thumbnail_320x180,
                },
            };
        }
        /// <summary>Returns the size value associated with the original image.</summary>
        public ModGalleryImageSize GetOriginalSize()
        {
            return ModGalleryImageSize.Original;
        }
    }
}
