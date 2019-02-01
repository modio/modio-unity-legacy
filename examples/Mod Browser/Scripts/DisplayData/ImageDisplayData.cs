using UnityEngine;

namespace ModIO.UI
{
    [System.Serializable]
    public struct ImageDisplayData
    {
        public static UserAvatarSize defaultAvatarSize = UserAvatarSize.Thumbnail_50x50;
        public static LogoSize defaultLogoSize = LogoSize.Thumbnail_320x180;
        public static ModGalleryImageSize defaultGalleryImageSize = ModGalleryImageSize.Thumbnail_320x180;

        public enum MediaType
        {
            None,
            ModLogo,
            ModGalleryImage,
            YouTubeThumbnail,
            UserAvatar,
        };

        public int ownerId;
        public MediaType mediaType;
        public string imageId;

        public Texture2D originalTexture;
        public Texture2D thumbnailTexture;

        public Texture2D texture;

        public int modId        { get { return ownerId; } set { ownerId = value; } }
        public int userId       { get { return ownerId; } set { ownerId = value; } }

        public string fileName  { get { return imageId; } set { imageId = value; } }
        public string youTubeId { get { return imageId; } set { imageId = value; } }

        public Texture2D GetImageTexture(bool original)
        {
            if(original)
            {
                return originalTexture;
            }
            else
            {
                return thumbnailTexture;
            }
        }
        public void SetImageTexture(bool original, Texture2D value)
        {
            if(original)
            {
                originalTexture = value;
            }
            else
            {
                thumbnailTexture = value;
            }
        }
    }
}
