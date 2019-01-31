using System;
using UnityEngine;

namespace ModIO.UI
{
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
