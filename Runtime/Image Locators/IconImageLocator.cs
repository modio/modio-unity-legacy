using Newtonsoft.Json;

using Debug = UnityEngine.Debug;

namespace ModIO
{
    [System.Serializable]
    public class IconImageLocator : IMultiSizeImageLocator<IconSize>
    {
        // ---------[ FIELDS ]---------
        /// <summary>
        /// Icon filename including extension.
        /// </summary>
        [JsonProperty("filename")]
        public string fileName;

        /// <summary>
        /// URL to the full-sized icon.
        /// </summary>
        [JsonProperty("original")]
        public string original;

        /// <summary>
        /// URL to the small icon thumbnail.
        /// </summary>
        [JsonProperty("thumb_64x64")]
        public string thumbnail_64x64;

        /// <summary>
        /// URL to the medium icon thumbnail.
        /// </summary>
        [JsonProperty("thumb_128x128")]
        public string thumbnail_128x128;

        /// <summary>
        /// URL to the large icon thumbnail.
        /// </summary>
        [JsonProperty("thumb_256x256")]
        public string thumbnail_256x256;

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
        public string GetSizeURL(IconSize size)
        {
            switch(size)
            {
                case IconSize.Original:
                {
                    return this.original;
                }
                case IconSize.Thumbnail_64x64:
                {
                    return this.thumbnail_64x64;
                }
                case IconSize.Thumbnail_128x128:
                {
                    return this.thumbnail_128x128;
                }
                case IconSize.Thumbnail_256x256:
                {
                    return this.thumbnail_256x256;
                }
                default:
                {
                    Debug.LogError("[mod.io] Unrecognized IconSize");
                    return string.Empty;
                }
            }
        }
        /// <summary>Returns all the URLs in the image locator.</summary>
        public SizeURLPair<IconSize>[] GetAllURLs()
        {
            return new SizeURLPair<IconSize>[] {
                new SizeURLPair<IconSize>() {
                    size = IconSize.Original,
                    url = this.original,
                },
                new SizeURLPair<IconSize>() {
                    size = IconSize.Thumbnail_64x64,
                    url = this.thumbnail_64x64,
                },
                new SizeURLPair<IconSize>() {
                    size = IconSize.Thumbnail_128x128,
                    url = this.thumbnail_128x128,
                },
                new SizeURLPair<IconSize>() {
                    size = IconSize.Thumbnail_256x256,
                    url = this.thumbnail_256x256,
                },
            };
        }
        /// <summary>Returns the size value associated with the original image.</summary>
        public IconSize GetOriginalSize()
        {
            return IconSize.Original;
        }
    }
}
