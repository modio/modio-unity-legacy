using Newtonsoft.Json;

namespace ModIO.API
{
    [System.Serializable]
    public struct LogoObject
    {
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
    }
}