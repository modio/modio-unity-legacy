using Int64 = System.Int64;

namespace ModIO.UI
{
    [System.Obsolete("No longer supported.")]
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

        public static ModfileDisplayData CreateFromModfile(Modfile modfile)
        {
            UnityEngine.Debug.Assert(modfile != null);

            ModfileDisplayData modfileData = new ModfileDisplayData() {
                modId = modfile.modId,
                modfileId = modfile.id,
                dateAdded = modfile.dateAdded,
                fileName = modfile.fileName,
                fileSize = modfile.fileSize,
                MD5 = modfile.fileHash.md5,
                version = modfile.version,
                changelog = modfile.changelog,
                metadataBlob = modfile.metadataBlob,
                virusScanDate = modfile.dateScanned,
                virusScanStatus = modfile.virusScanStatus,
                virusScanResult = modfile.virusScanResult,
                virusScanHash = modfile.virusScanHash,
            };

            return modfileData;
        }
    }

    [System.Obsolete("No longer supported.")]
    public abstract class ModfileDisplayComponent : UnityEngine.MonoBehaviour
    {
        public abstract event System.Action<ModfileDisplayComponent> onClick;

        public abstract ModfileDisplayData data { get; set; }

        public abstract void Initialize();
        public abstract void DisplayModfile(Modfile modfile);
        public abstract void DisplayLoading();
    }
}
