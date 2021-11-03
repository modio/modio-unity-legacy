using Newtonsoft.Json;

using Debug = UnityEngine.Debug;

namespace ModIO
{
    [System.Serializable]
    public class LogoImageLocator : IMultiSizeImageLocator<LogoSize>
    {
        // ---------[ FIELDS ]---------
        /// <summary>
        /// Logo filename including extension.
        /// </summmary>
        [JsonProperty("filename")]
        public string fileName;

        /// <summary>
        /// URL to the full-sized logo.
        /// </summmary>
        [JsonProperty("original")]
        public string original;

        /// <summary>
        /// URL to the small logo thumbnail.
        /// </summmary>
        [JsonProperty("thumb_320x180")]
        public string thumbnail_320x180;

        /// <summary>
        /// URL to the medium logo thumbnail.
        /// </summmary>
        [JsonProperty("thumb_640x360")]
        public string thumbnail_640x360;

        /// <summary>
        /// URL to the large logo thumbnail.
        /// </summmary>
        [JsonProperty("thumb_1280x720")]
        public string thumbnail_1280x720;

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
        public string GetSizeURL(LogoSize size)
        {
            switch(size)
            {
                case LogoSize.Original:
                {
                    return this.original;
                }
                case LogoSize.Thumbnail_320x180:
                {
                    return this.thumbnail_320x180;
                }
                case LogoSize.Thumbnail_640x360:
                {
                    return this.thumbnail_640x360;
                }
                case LogoSize.Thumbnail_1280x720:
                {
                    return this.thumbnail_1280x720;
                }
                default:
                {
                    Debug.LogError("[mod.io] Unrecognized LogoSize");
                    return string.Empty;
                }
            }
        }
        /// <summary>Returns all the URLs in the image locator.</summary>
        public SizeURLPair<LogoSize>[] GetAllURLs()
        {
            return new SizeURLPair<LogoSize>[] {
                new SizeURLPair<LogoSize>() {
                    size = LogoSize.Original,
                    url = this.original,
                },
                new SizeURLPair<LogoSize>() {
                    size = LogoSize.Thumbnail_320x180,
                    url = this.thumbnail_320x180,
                },
                new SizeURLPair<LogoSize>() {
                    size = LogoSize.Thumbnail_640x360,
                    url = this.thumbnail_640x360,
                },
                new SizeURLPair<LogoSize>() {
                    size = LogoSize.Thumbnail_1280x720,
                    url = this.thumbnail_1280x720,
                },
            };
        }
        /// <summary>Returns the size value associated with the original image.</summary>
        public LogoSize GetOriginalSize()
        {
            return LogoSize.Original;
        }
    }
}
