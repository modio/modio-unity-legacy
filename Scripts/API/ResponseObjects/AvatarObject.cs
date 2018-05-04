using Newtonsoft.Json;

namespace ModIO.API
{
    [System.Serializable]
    public struct AvatarObject
    {
        /// <summary>Avatar filename including extension.</summary>
        [JsonProperty("filename")]
        public string fileName;

        /// <summary>URL to the full-sized avatar.</summary>
        [JsonProperty("original")]
        public string fullSize;

        /// <summary>URL to the small avatar thumbnail.</summary>
        [JsonProperty("thumb_50x50")]
        public string thumbnail_50x50;

        /// <summary>URL to the medium avatar thumbnail.</summary>
        [JsonProperty("thumb_100x100")]
        public string thumbnail_100x100;
    }
}