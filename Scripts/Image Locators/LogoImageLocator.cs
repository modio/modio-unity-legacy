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
        public string GetFileName() { return this.fileName; }
        public string GetURL()      { return this.original; }
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
    }
}
