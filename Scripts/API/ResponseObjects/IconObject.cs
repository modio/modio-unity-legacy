using Newtonsoft.Json;

using Debug = UnityEngine.Debug;

namespace ModIO.API
{
    [System.Serializable]
    public struct IconObject : IMultiVersionImageLocator<GameIconVersion>
    {
        // ---------[ FIELDS ]---------
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

        // ---------[ INTERFACE IMPLEMENTATION ]---------
        public string GetFileName() { return this.fileName; }
        public string GetURL()      { return this.fullSize; }

        public string GetVersionURL(GameIconVersion version)
        {
            switch(version)
            {
                case GameIconVersion.FullSize:
                {
                    return this.fullSize;
                }
                case GameIconVersion.Thumbnail_64x64:
                {
                    return this.thumbnail_64x64;
                }
                case GameIconVersion.Thumbnail_128x128:
                {
                    return this.thumbnail_128x128;
                }
                case GameIconVersion.Thumbnail_256x256:
                {
                    return this.thumbnail_256x256;
                }
                default:
                {
                    Debug.LogError("[mod.io] Unrecognized GameIconVersion");
                    return string.Empty;
                }
            }
        }
    }
}