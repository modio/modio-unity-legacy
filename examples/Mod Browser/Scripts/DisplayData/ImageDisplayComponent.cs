using System;
using UnityEngine;

namespace ModIO.UI
{
    public abstract class ImageDataDisplayComponent : MonoBehaviour
    {
        public abstract event Action<ImageDataDisplayComponent> onClick;
        public abstract ImageDisplayData data { get; set; }
        public abstract bool useOriginal { get; set; }

        public abstract void Initialize();
        public abstract void DisplayLoading();
    }

    public abstract class ModMediaDisplayComponent : ImageDataDisplayComponent
    {
        public abstract void DisplayLogo(int modId, LogoImageLocator locator);
        public abstract void DisplayGalleryImage(int modId, GalleryImageLocator locator);
        public abstract void DisplayYouTubeThumbnail(int modId, string youTubeVideoId);
    }

    public abstract class ModGalleryImageDisplayComponent : ImageDataDisplayComponent
    {
        public abstract ModGalleryImageSize imageSize   { get; }

        public abstract void DisplayImage(int modId, GalleryImageLocator locator);
    }

    public abstract class UserAvatarDisplayComponent : ImageDataDisplayComponent
    {
        public abstract UserAvatarSize avatarSize { get; }

        public abstract void DisplayAvatar(int userId, AvatarImageLocator avatarLocator);
    }
}
