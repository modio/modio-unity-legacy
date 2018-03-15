namespace ModIO.API
{
    public class AddModfileParameters : RequestParameters
    {
        // ---------[ FIELDS ]---------
        // [REQUIRED] The binary file for the release. For compatibility you should ZIP the base folder of your mod, or if it is a collection of files which live in a pre-existing game folder, you should ZIP those files. Your file must meet the following conditions:
        public BinaryUpload filedata
        {
            set
            {
                this.SetBinaryData("filedata", value.fileName, value.data);
            }
        }
        // Version of the file release.
        public string version
        {
            set
            {
                this.SetStringValue("version", value);
            }
        }
        // Changelog of this release.
        public string changelog
        {
            set
            {
                this.SetStringValue("changelog", value);
            }
        }
        // Default value is true. Label this upload as the current release, this will change the modfile field on the parent mod to the id of this file after upload.
        public bool active
        {
            set
            {
                this.SetStringValue("active", value.ToString());
            }
        }
        // MD5 of the submitted file. When supplied the MD5 will be compared against the uploaded files MD5. If they don't match a 422 Unprocessible Entity error will be returned.
        public string filehash
        {
            set
            {
                this.SetStringValue("filehash", value);
            }
        }
        // Metadata stored by the game developer which may include properties such as what version of the game this file is compatible with. Metadata can also be stored as searchable key value pairs, and to the mod object.
        public string metadata_blob
        {
            set
            {
                this.SetStringValue("metadata_blob", value);
            }
        }
    }
}
