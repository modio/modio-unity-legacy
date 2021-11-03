namespace ModIO
{
    public static class GetUserModfilesFilterFields
    {
        // (integer) Unique id of the file.
        public const string id = "id";
        // (integer) Unique id of the mod.
        public const string modId = "mod_id";
        // (integer) Unix timestamp of date file was added.
        public const string dateAdded = "date_added";
        // (integer) Unix timestamp of date file was virus scanned.
        public const string dateScanned = "date_scanned";
        // (integer) Current virus scan status of the file. For newly added files that have yet to
        // be scanned this field will change frequently until a scan is complete:
        public const string virusStatus = "virus_status";
        // (integer) Was a virus detected:
        public const string virusPositive = "virus_positive";
        // (integer) Size of the file in bytes.
        public const string filesize = "filesize";
        // (string) MD5 hash of the file.
        public const string filehash = "filehash";
        // (string) Filename including extension.
        public const string filename = "filename";
        // (string) Release version this file represents.
        public const string version = "version";
        // (string) Changelog for the file.
        public const string changelog = "changelog";
        // (string) Metadata stored by the game developer for this file.
        public const string metadataBlob = "metadata_blob";
    }
}
