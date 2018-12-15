namespace ModIO.UI
{
    [System.Serializable]
    public struct ModDisplayData
    {
        public int modId;
        public int gameId;
        public ModStatus status;
        public ModVisibility visibility;
        public int dateAdded;
        public int dateUpdated;
        public int dateLive;
        public ModContentWarnings contentWarnings;
        public string homepageURL;
        public string name;
        public string nameId;
        public string summary;
        public string descriptionAsHTML;
        public string descriptionAsText;
        public string metadataBlob;
        public string profileURL;
        public MetadataKVP[] metadataKVPs;

        public UserDisplayData submittedBy;
        public ModfileDisplayData currentBuild;
        public ImageDisplayData[] media;
        public ModTagDisplayData[] tags;
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

    public abstract class ModProfileDisplayComponent : UnityEngine.MonoBehaviour
    {
        public abstract event System.Action<ModProfileDisplayComponent> onClick;

        public abstract ModDisplayData data { get; set; }

        public abstract void Initialize();
        public abstract void DisplayProfile(ModProfile profile,
                                            System.Collections.Generic.IEnumerable<ModTagCategory> tagCategories);
        public abstract void DisplayLoading();
    }
}
