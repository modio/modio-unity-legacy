using Newtonsoft.Json;

using Debug = UnityEngine.Debug;

namespace ModIO
{
    [System.Serializable]
    public class AvatarImageLocator : IMultiVersionImageLocator<UserAvatarVersion>
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

        // ---------[ INTERFACE IMPLEMENTATION ]---------
        public string GetFileName() { return this.fileName; }
        public string GetURL()      { return this.fullSize; }

        public string GetVersionURL(UserAvatarVersion version)
        {
            switch(version)
            {
                case UserAvatarVersion.FullSize:
                {
                    return this.fullSize;
                }
                case UserAvatarVersion.Thumbnail_50x50:
                {
                    return this.thumbnail_50x50;
                }
                case UserAvatarVersion.Thumbnail_100x100:
                {
                    return this.thumbnail_100x100;
                }
                default:
                {
                    Debug.LogError("[mod.io] Unrecognized UserAvatarVersion");
                    return string.Empty;
                }
            }
        }
    }
}