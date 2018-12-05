using Int64 = System.Int64;

namespace ModIO.UI
{
    [System.Serializable]
    public struct ModfileDisplayData
    {
        public int modfileId;
        public int modId;
        public int dateAdded;
        public string fileName;
        public Int64 fileSize;
        public FileHash fileHash;
        public string version;
        public string changelog;
        public string metadataBlob;
        public int dateScanned;
        public ModfileVirusScanStatus virusScanStatus;
        public ModfileVirusScanResult virusScanResult;
        public string virusScanHash;
    }

    public abstract class ModfileDataDisplayComponent : UnityEngine.MonoBehaviour
    {
        public abstract event System.Action<ModfileDataDisplayComponent> onClick;

        public abstract ModfileDisplayData data { get; set; }

        public abstract void Initialize();
        public abstract void DisplayModfile(Modfile modfile);
        public abstract void DisplayLoading();
    }
}
