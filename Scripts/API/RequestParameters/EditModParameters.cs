namespace ModIO.API
{
    public class EditModParameters : RequestParameters
    {
        // ---------[ CONSTRAINTS ]---------
        public const int SUMMARY_CHAR_LIMIT = 250;
        public const int DESCRIPTION_CHAR_MIN = 100;
        public const int DESCRIPTION_CHAR_LIMIT = 50000;
        public const int METADATA_CHAR_LIMIT = 50000;

        // ---------[ FIELDS ]---------
        // Status of a mod. The mod must have at least one uploaded modfile to be 'accepted' or 'archived' (best if this field is controlled by game admins, see status and visibility for details):
        public int status
        {
            set
            {
                this.SetStringValue("status", value);
            }
        }
        // Visibility of the mod (best if this field is controlled by mod admins, see status and visibility for details):
        public int visible
        {
            set
            {
                this.SetStringValue("visible", value);
            }
        }
        // Name of your mod. Cannot exceed 80 characters.
        public string name
        {
            set
            {
                this.SetStringValue("name", value);
            }
        }
        // Path for the mod on mod.io. For example: https://gamename.mod.io/mod-name-id-here. Cannot exceed 80 characters.
        public string name_id
        {
            set
            {
                this.SetStringValue("name_id", value);
            }
        }
        // Summary for your mod, giving a brief overview of what it's about. Cannot exceed 250 characters.
        public string summary
        {
            set
            {
                this.SetStringValue("summary", value);
            }
        }
        // Detailed description for your mod, which can include details such as 'About', 'Features', 'Install Instructions', 'FAQ', etc. HTML supported and encouraged.
        public string description
        {
            set
            {
                this.SetStringValue("description", value);
            }
        }
        // Official homepage for your mod. Must be a valid URL.
        public string homepage
        {
            set
            {
                this.SetStringValue("homepage", value);
            }
        }
        // Metadata stored by the game developer which may include properties as to how the item works, or other information you need to display. Metadata can also be stored as searchable key value pairs, and to individual mod files.
        public string metadata_blob
        {
            set
            {
                this.SetStringValue("metadata_blob", value);
            }
        }
        // Maximium number of subscribers for this mod. A value of 0 disables this limit.
        public int stock
        {
            set
            {
                this.SetStringValue("stock", value);
            }
        }
    }
}