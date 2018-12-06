namespace ModIO.UI
{
    [System.Serializable]
    public struct ModLogoDisplayData
    {
        public int modId;
        public string fileName;
        public UnityEngine.Texture2D texture;
    }

    public abstract class ModLogoDisplayComponent : UnityEngine.MonoBehaviour
    {
        public abstract event System.Action<ModLogoDisplayComponent> onClick;

        public abstract ModLogoDisplayData data { get; set; }

        public abstract void Initialize();
        public abstract void DisplayModLogo(int modId, LogoImageLocator locator);
        public abstract void DisplayLoading();
    }
}
