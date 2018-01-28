using System;

namespace ModIO.API
{
    [Serializable]
    public struct ModfileObject : IEquatable<ModfileObject>
    {
        // - Fields -
        public int id;  // Unique modfile id.
        public int mod_id;  // Unique mod id.
        public int date_added;  // Unix timestamp of date file was added.
        public int date_scanned;    // Unix timestamp of date file was virus scanned.
        public int virus_status;    // Current virus scan status of the file. For newly added files that have yet to be scanned this field will change frequently until a scan is complete:
        public int virus_positive;  // Was a virus detected:
        public string virustotal_hash; // VirusTotal proprietary hash to view the scan results.
        public int filesize;    // Size of the file in bytes.
        public FilehashObject filehash; // Contains filehash data.
        public string filename; // Filename including extension.
        public string version; // Release version this file represents.
        public string changelog; // Changelog for the file.
        public string metadata_blob; // Metadata stored by the game developer for this file.
        public ModfileDownloadObject download; // Contains download data.

        // - Equality Operators -
        public override int GetHashCode()
        {
            return this.id;
        }

        public override bool Equals(object obj)
        {
            return (obj is ModfileObject
                    && this.Equals((ModfileObject)obj));
        }

        public bool Equals(ModfileObject other)
        {
            return(this.id.Equals(other.id)
                   && this.mod_id.Equals(other.mod_id)
                   && this.date_added.Equals(other.date_added)
                   && this.date_scanned.Equals(other.date_scanned)
                   && this.virus_status.Equals(other.virus_status)
                   && this.virus_positive.Equals(other.virus_positive)
                   && this.virustotal_hash.Equals(other.virustotal_hash)
                   && this.filesize.Equals(other.filesize)
                   && this.filehash.Equals(other.filehash)
                   && this.filename.Equals(other.filename)
                   && this.version.Equals(other.version)
                   && this.changelog.Equals(other.changelog)
                   && this.metadata_blob.Equals(other.metadata_blob)
                   && this.download.Equals(other.download));
        }
    }

    [Serializable]
    public struct UnsubmittedModfileObject
    {
        // - Fields -
        public string version; // Version of the file release.
        public string changelog; // Changelog of this release.
        public bool active; // Default value is true. Label this upload as the current release, this will change the modfile field on the parent mod to the id of this file after upload.
        public string filehash; // MD5 of the submitted file. When supplied the MD5 will be compared against the uploaded files MD5. If they don't match a 422 Unprocessible Entity error will be returned.
        public string metadata_blob; // Metadata stored by the game developer which may include properties such as what version of the game this file is compatible with. Metadata can also be stored as searchable key value pairs, and to the mod object.
    }
}