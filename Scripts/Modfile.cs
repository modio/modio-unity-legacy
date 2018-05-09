using Newtonsoft.Json;

namespace ModIO
{
    public enum ModfileVirusScanStatus
    {
        NotScanned = 0,
        ScanComplete = 1,
        InProgress = 2,
        FileTooLarge = 3,
        FileNotFound = 4,
        ErrorScanning = 5,
    }

    public enum ModfileVirusScanResult
    {
        Clean = 0,
        FlaggedAsMalicious = 1,
    }

    [System.Serializable]
    public class ModfileStub
    {
        // ---------[ FIELDS ]---------
        /// <summary>Unique modfile id.</summary>
        [JsonProperty("id")]
        public int id;
        
        /// <summary>Unique mod id.</summary>
        [JsonProperty("mod_id")]
        public int modId;

        /// <summary>Unix timestamp of date file was added.</summary>
        [JsonProperty("date_added")]
        public int dateAdded;

        /// <summary>Size of the file in bytes.</summary>
        [JsonProperty("filesize")]
        public int filesize;

        /// <summary>Release version this file represents.</summary>
        [JsonProperty("version")]
        public string version;

        /// <summary>Changelog for the file.</summary>
        [JsonProperty("changelog")]
        public string changelog;

        /// <summary>Metadata stored by the game developer for this file.</summary>
        [JsonProperty("metadata_blob")]
        public string metadataBlob;
    }

    [System.Serializable]
    public class Modfile : ModfileStub
    {
        // ---------[ FIELDS ]---------
        /// <summary>Unix timestamp of date file was virus scanned.</summary>
        [JsonProperty("date_scanned")]
        public int dateScanned;

        /// <summary>Current virus scan status of the file. For newly added
        /// files that have yet to be scanned this field will change frequently
        /// until a scan is complete</summary>
        [JsonProperty("virus_status")]
        public ModfileVirusScanStatus virusScanStatus;

        /// <summary>Was a virus detected:</summary>
        [JsonProperty("virus_positive")]
        public ModfileVirusScanResult virusScanResult;

        /// <summary>VirusTotal proprietary hash to view the scan results.</summary>
        [JsonProperty("virustotal_hash")]
        public string virusScanHash;

        /// <summary>Contains filehash data.</summary>
        [JsonProperty("filehash")]
        public FileHash fileHash;

        /// <summary>Filename including extension.</summary>
        [JsonProperty("filename")]
        public string fileName;

        /// <summary>Contains download data.</summary>
        [JsonProperty("download")]
        public ModfileLocator downloadLocator;
    }
}