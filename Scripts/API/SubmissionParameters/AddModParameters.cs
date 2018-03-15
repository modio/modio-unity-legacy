using System.Collections.Generic;

using ModIO;

namespace ModIO.API
{
    public class AddModParameters : PostParameters
    {
        // ---------[ FIELDS ]---------
        // [REQUIRED] Name of your mod.
        public string name
        {
            set
            {
                this.SetStringParameter("name", value);
            }
        }
        // [REQUIRED] Image file which will represent your mods logo. Must be gif, jpg or png format and cannot exceed 8MB in filesize. Dimensions must be at least 640x360 and we recommended you supply a high resolution image with a 16 / 9 ratio. mod.io will use this image to make three thumbnails for the dimensions 320x180, 640x360 and 1280x720.
        public BinaryUpload logo
        {
            set
            {
                this.SetBinaryDataParameter("logo", value.fileName, value.data);
            }
        }
        // [REQUIRED] Summary for your mod, giving a brief overview of what it's about. Cannot exceed 250 characters.
        public string summary
        {
            set
            {
                this.SetStringParameter("summary", value);
            }
        }
        // Visibility of the mod (best if this field is controlled by mod admins, see status and visibility for details):
        public int visible
        {
            set
            {
                this.SetStringParameter("visible", value.ToString());
            }
        }
        // Path for the mod on mod.io. For example: https://gamename.mod.io/mod-name-id-here. If no name_id is specified the name will be used. For example: 'Stellaris Shader Mod' will become 'stellaris-shader-mod'. Cannot exceed 80 characters.
        public string name_id
        {
            set
            {
                this.SetStringParameter("name_id", value);
            }
        }
        // Detailed description for your mod, which can include details such as 'About', 'Features', 'Install Instructions', 'FAQ', etc. HTML supported and encouraged.
        public string description
        {
            set
            {
                this.SetStringParameter("description", value);
            }
        }
        // Official homepage for your mod. Must be a valid URL.
        public string homepage
        {
            set
            {
                this.SetStringParameter("homepage", value);
            }
        }
        // Maximium number of subscribers for this mod. A value of 0 disables this limit.
        public int stock
        {
            set
            {
                this.SetStringParameter("stock", value.ToString());
            }
        }
        // Metadata stored by the game developer which may include properties as to how the item works, or other information you need to display. Metadata can also be stored as searchable key value pairs, and to individual mod files.
        public string metadata_blob
        {
            set
            {
                this.SetStringParameter("metadata_blob", value);
            }
        }
        // An array of strings that represent what the mod has been tagged as. Only tags that are supported by the parent game can be applied. To determine what tags are eligible, see the tags values within tag_options column on the parent Game Object.
        public string[] tags
        {
            set
            {
                this.SetStringArrayParameter("tags[]", value);
            }
        }
    }
}