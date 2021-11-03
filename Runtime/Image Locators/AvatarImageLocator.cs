using Newtonsoft.Json;

using Debug = UnityEngine.Debug;

namespace ModIO
{
    [System.Serializable]
    public class AvatarImageLocator : IMultiSizeImageLocator<UserAvatarSize>
    {
        /// <summary>
        /// Avatar filename including extension.
        /// </summary>
        [JsonProperty("filename")]
        public string fileName;

        /// <summary>
        /// URL to the full-sized avatar.
        /// </summary>
        [JsonProperty("original")]
        public string original;

        /// <summary>
        /// URL to the small avatar thumbnail.
        /// </summary>
        [JsonProperty("thumb_50x50")]
        public string thumbnail_50x50;

        /// <summary>
        /// URL to the medium avatar thumbnail.
        /// </summary>
        [JsonProperty("thumb_100x100")]
        public string thumbnail_100x100;

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
        public string GetSizeURL(UserAvatarSize size)
        {
            switch(size)
            {
                case UserAvatarSize.Original:
                {
                    return this.original;
                }
                case UserAvatarSize.Thumbnail_50x50:
                {
                    return this.thumbnail_50x50;
                }
                case UserAvatarSize.Thumbnail_100x100:
                {
                    return this.thumbnail_100x100;
                }
                default:
                {
                    Debug.LogError("[mod.io] Unrecognized UserAvatarSize");
                    return string.Empty;
                }
            }
        }
        /// <summary>Returns all the URLs in the image locator.</summary>
        public SizeURLPair<UserAvatarSize>[] GetAllURLs()
        {
            return new SizeURLPair<UserAvatarSize>[] {
                new SizeURLPair<UserAvatarSize>() {
                    size = UserAvatarSize.Original,
                    url = this.original,
                },
                new SizeURLPair<UserAvatarSize>() {
                    size = UserAvatarSize.Thumbnail_50x50,
                    url = this.thumbnail_50x50,
                },
                new SizeURLPair<UserAvatarSize>() {
                    size = UserAvatarSize.Thumbnail_100x100,
                    url = this.thumbnail_100x100,
                },
            };
        }
        /// <summary>Returns the size value associated with the original image.</summary>
        public UserAvatarSize GetOriginalSize()
        {
            return UserAvatarSize.Original;
        }
    }
}
