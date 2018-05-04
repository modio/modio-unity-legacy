namespace ModIO.API
{
    [System.Serializable]
    public class ModfileStub
    {
        // Unique modfile id.
        public int id;
        // Release version this file represents.
        public string version;
        // Size of the file in bytes.
        public int filesize;
    }

    [System.Serializable]
    public class ModfileObject : ModfileStub
    {
        // Unique mod id.
        public int mod_id;
        //Unix timestamp of date file was added.
        public int date_added;
        //Unix timestamp of date file was virus scanned.
        public int date_scanned;
        //Current virus scan status of the file. For newly added files that have yet to be scanned this field will change frequently until a scan is complete:
        public int virus_status;
        //Was a virus detected:
        public int virus_positive;
        //VirusTotal proprietary hash to view the scan results.
        public string virustotal_hash;
        //Filename including extension.
        public string filename;
        //Changelog for the file.
        public string changelog;
        //Metadata stored by the game developer for this file.
        public string metadata_blob;
        // Contains filehash data.
        public FilehashObject filehash;
        // Contains download data.
        public DownloadObject download;
    }
}