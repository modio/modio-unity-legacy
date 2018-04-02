namespace ModIO.API
{
    [System.Serializable]
    public struct ModfileObject
    {
        //Unique modfile id.
        public readonly int id;
        //Unique mod id.
        public readonly int mod_id;
        //Unix timestamp of date file was added.
        public readonly int date_added;
        //Unix timestamp of date file was virus scanned.
        public readonly int date_scanned;
        //Current virus scan status of the file. For newly added files that have yet to be scanned this field will change frequently until a scan is complete:
        public readonly int virus_status;
        //Was a virus detected:
        public readonly int virus_positive;
        //VirusTotal proprietary hash to view the scan results.
        public readonly string virustotal_hash;
        //Size of the file in bytes.
        public readonly int filesize;
        //Filename including extension.
        public readonly string filename;
        //Release version this file represents.
        public readonly string version;
        //Changelog for the file.
        public readonly string changelog;
        //Metadata stored by the game developer for this file.
        public readonly string metadata_blob;
        // Contains filehash data.
        public readonly FilehashObject filehash;
        // Contains download data.
        public readonly DownloadObject download;
    }
}