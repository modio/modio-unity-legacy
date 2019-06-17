using UnityEngine;

namespace ModIO.UI
{
    [System.Serializable]
    public struct ImageDisplayData
    {
        public static UserAvatarSize avatarThumbnailSize = UserAvatarSize.Thumbnail_50x50;
        public static LogoSize logoThumbnailSize = LogoSize.Thumbnail_320x180;
        public static ModGalleryImageSize galleryThumbnailSize = ModGalleryImageSize.Thumbnail_320x180;

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

        /// <summary>The URL for the original version of the image.</summary>
        public string originalURL;
        /// <summary>The URL for the thumbnail version of the image.</summary>
        public string thumbnailURL;

        public Texture2D originalTexture;
        public Texture2D thumbnailTexture;

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

        /// <summary>Returns the image URL depending on whether the original or thumbnail is desired.</summary>
        public string GetImageURL(bool original)
        {
            if(original){ return this.originalURL; }
            else        { return this.thumbnailURL; }
        }

        // ---------[ GENERATION ]---------
        /// <summary>Creates the ImageDisplayData for a mod logo.</summary>
        public static ImageDisplayData CreateForModLogo(int modId, LogoImageLocator locator)
        {
            ImageDisplayData retVal = new ImageDisplayData()
            {
                ownerId = modId,
                mediaType = MediaType.ModLogo,
                imageId = locator.GetFileName(),
                originalURL = locator.GetSizeURL(LogoSize.Original),
                thumbnailURL = locator.GetSizeURL(ImageDisplayData.logoThumbnailSize),
                originalTexture = null,
                thumbnailTexture = null,
            };

            return retVal;
        }
        /// <summary>Creates the ImageDisplayData for a mod gallery image.</summary>
        public static ImageDisplayData CreateForModGalleryImage(int modId, GalleryImageLocator locator)
        {
            ImageDisplayData retVal = new ImageDisplayData()
            {
                ownerId = modId,
                mediaType = MediaType.ModGalleryImage,
                imageId = locator.GetFileName(),
                originalURL = locator.GetSizeURL(ModGalleryImageSize.Original),
                thumbnailURL = locator.GetSizeURL(ImageDisplayData.galleryThumbnailSize),
                originalTexture = null,
                thumbnailTexture = null,
            };

            return retVal;
        }
        /// <summary>Creates the ImageDisplayData for a YouTube thumbnail.</summary>
        public static ImageDisplayData CreateForYouTubeThumbnail(int modId, string youTubeId)
        {
            string url = Utility.GenerateYouTubeThumbnailURL(youTubeId);

            ImageDisplayData retVal = new ImageDisplayData()
            {
                ownerId = modId,
                mediaType = MediaType.YouTubeThumbnail,
                imageId = youTubeId,
                originalURL = url,
                thumbnailURL = url,
                originalTexture = null,
                thumbnailTexture = null,
            };

            return retVal;
        }
        /// <summary>Creates the ImageDisplayData for a user avatar.</summary>
        public static ImageDisplayData CreateForUserAvatar(int userId, AvatarImageLocator locator)
        {
            ImageDisplayData retVal = new ImageDisplayData()
            {
                ownerId = userId,
                mediaType = MediaType.UserAvatar,
                imageId = locator.GetFileName(),
                originalURL = locator.GetSizeURL(UserAvatarSize.Original),
                thumbnailURL = locator.GetSizeURL(ImageDisplayData.avatarThumbnailSize),
                originalTexture = null,
                thumbnailTexture = null,
            };

            return retVal;
        }

        // ---------[ EQUALITY OVERRIDES ]---------
        public override bool Equals(object obj)
        {
            if(!(obj is ImageDisplayData))
            {
                return false;
            }

            var other = (ImageDisplayData)obj;
            bool isEqual = (this.ownerId == other.ownerId
                            && this.mediaType == other.mediaType
                            && this.imageId == other.imageId);
            return isEqual;
        }

        public override int GetHashCode()
        {
            int idFactor = (string.IsNullOrEmpty(this.imageId)
                            ? 1
                            : this.imageId.GetHashCode());
            return (this.ownerId << 2) ^ idFactor;
        }
    }
}
