namespace ModIO.UI
{
    [System.Serializable]
    public struct ModDisplayData
    {
        public int modId;
        public ModProfileDisplayData    profile;
        public UserDisplayData          submittedBy;
        public ModfileDisplayData       currentBuild;
        public ImageDisplayData[]       media;
        public ModTagDisplayData[]      tags;
    }

    public abstract class ModDisplayComponent : UnityEngine.MonoBehaviour
    {
        public abstract event System.Action<ModDisplayComponent> onClick;

        public abstract ModDisplayData data { get; set; }

        public abstract void Initialize();
        public abstract void DisplayProfile(ModProfile profile,
                                            System.Collections.Generic.IEnumerable<ModTagCategory> tagCategories);
        public abstract void DisplayLoading();
    }
}
