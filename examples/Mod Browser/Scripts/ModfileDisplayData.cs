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
        public string MD5;
        public string version;
        public string changelog;
        public string metadataBlob;
        public int virusScanDate;
        public ModfileVirusScanStatus virusScanStatus;
        public ModfileVirusScanResult virusScanResult;
        public string virusScanHash;
    }

    public abstract class ModfileDisplayComponent : UnityEngine.MonoBehaviour
    {
        public abstract event System.Action<ModfileDisplayComponent> onClick;

        public abstract ModfileDisplayData data { get; set; }

        public abstract void Initialize();
        public abstract void DisplayModfile(Modfile modfile);
        public abstract void DisplayLoading();
    }
}
