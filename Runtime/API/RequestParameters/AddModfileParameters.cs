namespace ModIO.API
{
    public class AddModfileParameters : RequestParameters
    {
        // ---------[ FIELDS ]---------
        // [REQUIRED] The binary file for the release. For compatibility you should ZIP the base
        // folder of your mod, or if it is a collection of files which live in a pre-existing game
        // folder, you should ZIP those files. Your file must meet the following conditions:
        // - File must be zipped and cannot exceed 10GB in filesize.
        // - Mods which span multiple game directories are not supported unless the game manages
        //   this.
        // - Mods which overwrite files are not supported unless the game manages this.
        public BinaryUpload zippedBinaryData
        {
            set {
                this.SetBinaryData("filedata", value.fileName, value.data);
            }
        }

        // Version of the file release.
        public string version
        {
            set {
                this.SetStringValue("version", value);
            }
        }

        // Changelog of this release.
        public string changelog
        {
            set {
                this.SetStringValue("changelog", value);
            }
        }

        // Label this upload as the current release, this will change the modfile field on the
        // parent mod to the id of this file after upload.
        // This defaults to true.
        public bool isActiveBuild
        {
            set {
                this.SetStringValue("active", value.ToString());
            }
        }

        // MD5 of the submitted file. When supplied the MD5 will be compared against the uploaded
        // file's MD5. If they don't match a 422 Unprocessible Entity error will be returned.
        public string fileHash
        {
            set {
                this.SetStringValue("filehash", value);
            }
        }

        // Metadata stored by the game developer which may include properties such as which version
        // of the game this file is compatible with.
        public string metadataBlob
        {
            set {
                this.SetStringValue("metadata_blob", value);
            }
        }
    }
}
