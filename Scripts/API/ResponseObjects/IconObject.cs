using Newtonsoft.Json;

namespace ModIO.API
{
    [System.Serializable]
    public struct IconObject
    {
        ///<summary>Icon filename including extension.</summary>
        [JsonProperty("filename")]
        public string fileName;

        ///<summary>URL to the full-sized icon.</summary>
        [JsonProperty("original")]
        public string fullSize;

        ///<summary>URL to the small icon thumbnail.</summary>
        [JsonProperty("thumb_64x64")]
        public string thumbnail_64x64;

        ///<summary>URL to the medium icon thumbnail.</summary>
        [JsonProperty("thumb_128x128")]
        public string thumbnail_128x128;

        ///<summary>URL to the large icon thumbnail.</summary>
        [JsonProperty("thumb_256x256")]
        public string thumbnail_256x256;
    }
}