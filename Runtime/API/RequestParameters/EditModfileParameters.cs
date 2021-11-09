namespace ModIO.API
{
    public class EditModfileParameters : RequestParameters
    {
        // ---------[ FIELDS ]---------
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
        // NOTE: If the active parameter causes the parent mods modfile parameter to change, a
        // MODFILE_CHANGED event will be fired, so game clients know there is an update available
        // for this mod.
        public bool isActiveBuild
        {
            set {
                this.SetStringValue("active", value);
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
