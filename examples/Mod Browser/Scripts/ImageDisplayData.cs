using System;

namespace ModIO.UI
{
    [System.Serializable]
    public struct ImageDisplayData
    {
        public int modId;

        public string imageId;
        public string fileName  { get { return imageId; } set { imageId = value; } }
        public string youTubeId { get { return imageId; } set { imageId = value; } }

        public UnityEngine.Texture2D texture;
    }

    public abstract class ModMediaDisplayComponent : UnityEngine.MonoBehaviour
    {
        public enum DataType
        {
            Logo,
            GalleryImage,
            YouTubeThumbnail
        };

        public abstract event Action<ModMediaDisplayComponent> logoClicked;
        public abstract event Action<ModMediaDisplayComponent> galleryImageClicked;
        public abstract event Action<ModMediaDisplayComponent> youTubeThumbnailClicked;

        public abstract LogoSize logoSize                       { get; }
        public abstract ModGalleryImageSize galleryImageSize    { get; }

        // public abstract Image image { get; }

        public abstract DataType mediaType              { get; set; }
        public abstract ImageDisplayData data           { get; set; }

        public abstract void Initialize();
        public abstract void DisplayLogo(int modId, LogoImageLocator locator);
        public abstract void DisplayGalleryImage(int modId, GalleryImageLocator locator);
        public abstract void DisplayYouTubeThumbnail(int modId, string youTubeVideoId);
        public abstract void DisplayLoading();
    }

    public abstract class ModLogoDisplayComponent : UnityEngine.MonoBehaviour
    {
        public abstract event System.Action<ModLogoDisplayComponent> onClick;

        public abstract LogoSize logoSize       { get; }
        public abstract ImageDisplayData data   { get; set; }

        public abstract void Initialize();
        public abstract void DisplayLogo(int modId, LogoImageLocator locator);
        public abstract void DisplayLoading();
    }

    public abstract class ModGalleryImageDisplayComponent : UnityEngine.MonoBehaviour
    {
        public abstract event System.Action<ModGalleryImageDisplayComponent> onClick;

        public abstract ModGalleryImageSize imageSize   { get; }
        public abstract ImageDisplayData data           { get; set; }

        public abstract void Initialize();
        public abstract void DisplayImage(int modId, GalleryImageLocator locator);
        public abstract void DisplayLoading();
    }

    public abstract class YouTubeThumbnailDisplayComponent : UnityEngine.MonoBehaviour
    {
        public abstract event System.Action<YouTubeThumbnailDisplayComponent> onClick;

        public abstract ImageDisplayData data           { get; set; }

        public abstract void Initialize();
        public abstract void DisplayThumbnail(int modId, string youTubeVideoId);
        public abstract void DisplayLoading();
    }
}
