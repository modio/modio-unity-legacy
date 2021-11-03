using UnityEngine;

namespace ModIO.UI
{
    [System.Obsolete("No longer supported.")]
    [System.Serializable]
    public struct ImageDisplayData
    {
        public static UserAvatarSize avatarThumbnailSize = UserAvatarSize.Thumbnail_50x50;
        public static LogoSize logoThumbnailSize = LogoSize.Thumbnail_320x180;
        public static ModGalleryImageSize galleryThumbnailSize =
            ModGalleryImageSize.Thumbnail_320x180;

        public int ownerId;
        [UnityEngine.Serialization.FormerlySerializedAs("mediaType")]
        public ImageDescriptor descriptor;
        public string imageId;

        /// <summary>The URL for the original version of the image.</summary>
        public string originalURL;
        /// <summary>The URL for the thumbnail version of the image.</summary>
        public string thumbnailURL;

        public int modId
        {
            get {
                return ownerId;
            }
            set {
                ownerId = value;
            }
        }
        public int userId
        {
            get {
                return ownerId;
            }
            set {
                ownerId = value;
            }
        }

        public string fileName
        {
            get {
                return imageId;
            }
            set {
                imageId = value;
            }
        }
        public string youTubeId
        {
            get {
                return imageId;
            }
            set {
                imageId = value;
            }
        }

        /// <summary>Returns the image URL depending on whether the original or thumbnail is
        /// desired.</summary>
        public string GetImageURL(bool original)
        {
            if(original)
            {
                return this.originalURL;
            }
            else
            {
                return this.thumbnailURL;
            }
        }

        // ---------[ GENERATION ]---------
        /// <summary>Creates the ImageDisplayData for a mod logo.</summary>
        public static ImageDisplayData CreateForModLogo(int modId, LogoImageLocator locator)
        {
            ImageDisplayData retVal = new ImageDisplayData() {
                ownerId = modId,
                descriptor = ImageDescriptor.ModLogo,
                imageId = locator.GetFileName(),
                originalURL = locator.GetSizeURL(LogoSize.Original),
                thumbnailURL = locator.GetSizeURL(ImageDisplayData.logoThumbnailSize),
            };

            return retVal;
        }
        /// <summary>Creates the ImageDisplayData for a mod gallery image.</summary>
        public static ImageDisplayData CreateForModGalleryImage(int modId,
                                                                GalleryImageLocator locator)
        {
            ImageDisplayData retVal = new ImageDisplayData() {
                ownerId = modId,
                descriptor = ImageDescriptor.ModGalleryImage,
                imageId = locator.GetFileName(),
                originalURL = locator.GetSizeURL(ModGalleryImageSize.Original),
                thumbnailURL = locator.GetSizeURL(ImageDisplayData.galleryThumbnailSize),
            };

            return retVal;
        }
        /// <summary>Creates the ImageDisplayData for a YouTube thumbnail.</summary>
        public static ImageDisplayData CreateForYouTubeThumbnail(int modId, string youTubeId)
        {
            string url = Utility.GenerateYouTubeThumbnailURL(youTubeId);

            ImageDisplayData retVal = new ImageDisplayData() {
                ownerId = modId,     descriptor = ImageDescriptor.YouTubeThumbnail,
                imageId = youTubeId, originalURL = url,
                thumbnailURL = url,
            };

            return retVal;
        }
        /// <summary>Creates the ImageDisplayData for a user avatar.</summary>
        public static ImageDisplayData CreateForUserAvatar(int userId, AvatarImageLocator locator)
        {
            ImageDisplayData retVal = new ImageDisplayData() {
                ownerId = userId,
                descriptor = ImageDescriptor.UserAvatar,
                imageId = locator.GetFileName(),
                originalURL = locator.GetSizeURL(UserAvatarSize.Original),
                thumbnailURL = locator.GetSizeURL(ImageDisplayData.avatarThumbnailSize),
            };

            return retVal;
        }

        // ---------[ OBSOLETE ]---------
        [System.Obsolete("Use ModIO.UI.ImageDescriptor instead.")]
        public enum MediaType
        {
            None = 0,
            ModLogo,
            ModGalleryImage,
            YouTubeThumbnail,
            UserAvatar,
        }
        [System.Obsolete("Use ImageDisplayData.descriptor instead.")]
        public MediaType mediaType
        {
            get {
                return (MediaType)this.descriptor;
            }
            set {
                this.descriptor = (ImageDescriptor)value;
            }
        }

        [System.Obsolete("Images are now to be fetched via ImageRequestManager")] public Texture2D
            originalTexture;
        [System.Obsolete("Images are now to be fetched via ImageRequestManager")]
        public Texture2D thumbnailTexture;
        [System.Obsolete("Images are now to be fetched via ImageRequestManager")]
        public Texture2D GetImageTexture(bool original)
        {
            return null;
        }
        [System.Obsolete("Images are now to be fetched via ImageRequestManager")]
        public void SetImageTexture(bool original, Texture2D value) {}
    }
}
