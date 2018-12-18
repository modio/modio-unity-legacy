using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModIO.UI
{
    [System.Serializable]
    public struct ImageDisplayData
    {
        public enum MediaType
        {
            ModLogo,
            ModGalleryImage,
            YouTubeThumbnail,
            UserAvatar,
        };

        public int ownerId;
        public MediaType mediaType;
        public string imageId;
        public Texture2D texture;

        public int modId        { get { return ownerId; } set { ownerId = value; } }
        public int userId       { get { return ownerId; } set { ownerId = value; } }

        public string fileName  { get { return imageId; } set { imageId = value; } }
        public string youTubeId { get { return imageId; } set { imageId = value; } }
    }

    public abstract class ImageDataDisplayComponent : MonoBehaviour
    {
        public abstract event Action<ImageDataDisplayComponent> onClick;
        public abstract ImageDisplayData data { get; set; }

        public abstract void Initialize();
        public abstract void DisplayLoading();
    }

    public abstract class ModMediaDisplayComponent : ImageDataDisplayComponent
    {
        public abstract LogoSize logoSize                       { get; }
        public abstract ModGalleryImageSize galleryImageSize    { get; }

        public abstract void DisplayLogo(int modId, LogoImageLocator locator);
        public abstract void DisplayGalleryImage(int modId, GalleryImageLocator locator);
        public abstract void DisplayYouTubeThumbnail(int modId, string youTubeVideoId);
    }

    public abstract class ModLogoDisplayComponent : ImageDataDisplayComponent
    {
        public abstract LogoSize logoSize       { get; }

        public abstract void DisplayLogo(int modId, LogoImageLocator locator);
    }

    public abstract class ModGalleryImageDisplayComponent : ImageDataDisplayComponent
    {
        public abstract ModGalleryImageSize imageSize   { get; }

        public abstract void DisplayImage(int modId, GalleryImageLocator locator);
    }

    public abstract class YouTubeThumbnailDisplayComponent : ImageDataDisplayComponent
    {
        public abstract void DisplayThumbnail(int modId, string youTubeVideoId);
    }

    public abstract class UserAvatarDisplayComponent : ImageDataDisplayComponent
    {
        public abstract UserAvatarSize avatarSize { get; }

        public abstract void DisplayAvatar(int userId, AvatarImageLocator avatarLocator);
    }
}
