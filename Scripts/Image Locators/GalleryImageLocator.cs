using Newtonsoft.Json;

using Debug = UnityEngine.Debug;

namespace ModIO
{
    [System.Serializable]
    public class GalleryImageLocator : IMultiVersionImageLocator<ModGalleryImageVersion>
    {
        // ---------[ FIELDS ]---------
        ///<summary>Image filename including extension.</summary>
        [JsonProperty("filename")]
        public string fileName;

        ///<summary>URL to the full-sized image.</summary>
        [JsonProperty("original")]
        public string fullSize;

        ///<summary>URL to the image thumbnail.</summary>
        [JsonProperty("thumb_320x180")]
        public string thumbnail_320x180;

        // ---------[ INTERFACE IMPLEMENTATION ]---------
        public string GetFileName() { return this.fileName; }
        public string GetURL()      { return this.fullSize; }

        public string GetVersionURL(ModGalleryImageVersion version)
        {
            switch(version)
            {
                case ModGalleryImageVersion.FullSize:
                {
                    return this.fullSize;
                }
                case ModGalleryImageVersion.Thumbnail_320x180:
                {
                    return this.thumbnail_320x180;
                }
                default:
                {
                    Debug.LogError("[mod.io] Unrecognized ModGalleryImageVersion");
                    return string.Empty;
                }
            }
        }
    }
}
