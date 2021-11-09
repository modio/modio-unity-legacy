namespace ModIO.API
{
    public class EditModParameters : RequestParameters
    {
        // ---------[ CONSTRAINTS ]---------
        public const int NAME_CHAR_LIMIT = 80;
        public const int NAMEID_CHAR_LIMIT = 80;
        public const int SUMMARY_CHAR_LIMIT = 250;
        public const int DESCRIPTION_CHAR_MIN = 100;
        public const int DESCRIPTION_CHAR_LIMIT = 50000;
        public const int METADATA_CHAR_LIMIT = 50000;

        // ---------[ FIELDS ]---------
        // Status of a mod. The mod must have at least one uploaded modfile to be 'accepted' or
        // 'archived' (best if this field is controlled by game admins, see status and visibility
        // for details)
        public ModStatus status
        {
            set {
                this.SetStringValue("status", (int)value);
            }
        }

        // Visibility of the mod (best if this field is controlled by mod admins, see status and
        // visibility for details):
        public ModVisibility visibility
        {
            set {
                this.SetStringValue("visible", (int)value);
            }
        }

        // Name of your mod. Cannot exceed 80 characters.
        public string name
        {
            set {
                this.SetStringValue("name", value);
            }
        }

        // Path for the mod on mod.io. For example: https://gamename.mod.io/mod-name-id-here.
        // Cannot exceed 80 characters.
        public string nameId
        {
            set {
                this.SetStringValue("name_id", value);
            }
        }

        // Summary for your mod, giving a brief overview of what it's about. Cannot exceed 250
        // characters.
        public string summary
        {
            set {
                this.SetStringValue("summary", value);
            }
        }

        // Detailed description for your mod, which can include details such as 'About', 'Features',
        // 'Install Instructions', 'FAQ', etc. HTML supported and encouraged.
        public string descriptionAsHTML
        {
            set {
                this.SetStringValue("description", value);
            }
        }

        // Official homepage for your mod. Must be a valid URL.
        public string homepageURL
        {
            set {
                this.SetStringValue("homepage_url", value);
            }
        }

        // Choose if this mod contains any of the following mature content. Note: The value of this
        // field will default to 0 unless the parent game allows you to flag mature content (see
        // maturity_options field in Game Object).
        public ModContentWarnings contentWarnings
        {
            set {
                this.SetStringValue("maturity_option", (int)value);
            }
        }

        // Metadata stored by the game developer which may include properties as to how the item
        // works, or other information you need to display. Metadata can also be stored as
        // searchable key value pairs, and to individual mod files.
        public string metadataBlob
        {
            set {
                this.SetStringValue("metadata_blob", value);
            }
        }
    }
}
