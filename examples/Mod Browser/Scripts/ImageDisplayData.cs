namespace ModIO.UI
{
    [System.Serializable]
    public struct ImageDisplayData
    {
        public int modId;

        public string imageId;
        public string fileName  { get { return imageId; } set { imageId = value; } }
        public string youTubeID { get { return imageId; } set { imageId = value; } }

        public UnityEngine.Texture2D texture;
    }

    public abstract class ModLogoDisplayComponent : UnityEngine.MonoBehaviour
    {
        public abstract event System.Action<ModLogoDisplayComponent> onClick;

        public abstract LogoSize logoSize       { get; set; }
        public abstract ImageDisplayData data   { get; set; }

        public abstract void Initialize();
        public abstract void DisplayLogo(int modId, LogoImageLocator locator);
        public abstract void DisplayLoading();
    }

    public abstract class ModGalleryImageDisplayComponent : UnityEngine.MonoBehaviour
    {
        public abstract event System.Action<ModGalleryImageDisplayComponent> onClick;

        public abstract ModGalleryImageSize logoSize    { get; set; }
        public abstract ImageDisplayData data           { get; set; }

        public abstract void Initialize();
        public abstract void DisplayGalleryImage(int modId, GalleryImageLocator locator);
        public abstract void DisplayLoading();
    }

    public abstract class YouTubeThumbnailDisplayComponent : UnityEngine.MonoBehaviour
    {
        public abstract event System.Action<YouTubeThumbnailDisplayComponent> onClick;

        public abstract ImageDisplayData data           { get; set; }

        public abstract void Initialize();
        public abstract void DisplayGalleryImage(int modId, string youTubeVideoId);
        public abstract void DisplayLoading();
    }
}
