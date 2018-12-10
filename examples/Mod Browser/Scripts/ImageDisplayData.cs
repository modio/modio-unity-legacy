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
        public abstract void DisplayModLogo(int modId, LogoImageLocator locator);
        public abstract void DisplayLoading();
    }
}
