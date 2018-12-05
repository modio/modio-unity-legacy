namespace ModIO.UI
{
    [System.Serializable]
    public struct ModTagDisplayData
    {
        public string tagName;
        public string categoryName;
    }

    public abstract class ModTagDisplayComponent : UnityEngine.MonoBehaviour
    {
        public abstract event System.Action<ModTagDisplayComponent> onClick;

        public abstract ModTagDisplayData data { get; set; }

        public abstract void Initialize();
        public abstract void DisplayModTag(string tagName, string categoryName);
        public abstract void DisplayModTag(ModTag tag, string categoryName);
        public abstract void DisplayLoading();
    }
}
