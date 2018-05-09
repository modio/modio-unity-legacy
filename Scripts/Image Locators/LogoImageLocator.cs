using Newtonsoft.Json;

using Debug = UnityEngine.Debug;

namespace ModIO
{
    public enum LogoVersion
    {
        FullSize = 0,
        Thumbnail_320x180,
        Thumbnail_640x360,
        Thumbnail_1280x720,
    }

    [System.Serializable]
    public class LogoImageLocator : IMultiVersionImageLocator<LogoVersion>
    {
        // ---------[ FIELDS ]---------
        /// <summary> Logo filename including extension. </summmary>
        [JsonProperty("filename")]
        public string fileName;

        /// <summary> URL to the full-sized logo. </summmary>
        [JsonProperty("original")]
        public string fullSize;

        /// <summary> URL to the small logo thumbnail. </summmary>
        [JsonProperty("thumb_320x180")]
        public string thumbnail_320x180;

        /// <summary> URL to the medium logo thumbnail. </summmary>
        [JsonProperty("thumb_640x360")]
        public string thumbnail_640x360;

        /// <summary> URL to the large logo thumbnail. </summmary>
        [JsonProperty("thumb_1280x720")]
        public string thumbnail_1280x720;

        // ---------[ INTERFACE IMPLEMENTATION ]---------
        public string GetFileName() { return this.fileName; }
        public string GetURL()      { return this.fullSize; }
        public string GetVersionURL(LogoVersion version)
        {
            switch(version)
            {
                case LogoVersion.FullSize:
                {
                    return this.fullSize;
                }
                case LogoVersion.Thumbnail_320x180:
                {
                    return this.thumbnail_320x180;
                }
                case LogoVersion.Thumbnail_640x360:
                {
                    return this.thumbnail_640x360;
                }
                case LogoVersion.Thumbnail_1280x720:
                {
                    return this.thumbnail_1280x720;
                }
                default:
                {
                    Debug.LogError("[mod.io] Unrecognized LogoVersion");
                    return string.Empty;
                }
            }
        }
    }
}