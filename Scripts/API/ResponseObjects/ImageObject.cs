using Newtonsoft.Json;

namespace ModIO.API
{
    [System.Serializable]
    public struct ImageObject
    {
        ///<summary>Image filename including extension.</summary>
        [JsonProperty("filename")]
        public string fileName;

        ///<summary>URL to the full-sized image.</summary>
        [JsonProperty("original")]
        public string fullSize;

        ///<summary>URL to the image thumbnail.</summary>
        [JsonProperty("thumb_320x180")]
        public string thumbnail_320x180;
    }
}
