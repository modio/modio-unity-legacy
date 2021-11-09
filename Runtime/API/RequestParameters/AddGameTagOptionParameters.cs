namespace ModIO.API
{
    public class AddGameTagOptionParameters : RequestParameters
    {
        // ---------[ FIELDS ]---------
        // [REQUIRED] Name of the tag group, for example you may want to have 'Difficulty' as the
        /// name with 'Easy', 'Medium' and 'Hard' as the tag values.
        public string name
        {
            set {
                this.SetStringValue("name", value);
            }
        }

        // [REQUIRED] Determines whether you allow users to only select one tag (dropdown) or
        // multiple tags (checkbox).
        public bool isMultiTagCategory
        {
            set {
                this.SetStringValue("type",
                                    (value == true
                                         ? ModTagCategory.APIOBJECT_VALUESTRING_ISMULTITAG
                                         : ModTagCategory.APIOBJECT_VALUESTRING_ISSINGLETAG));
            }
        }

        // [REQUIRED] Array of tags mod creators can choose to apply to their profiles.
        public string[] tags
        {
            set {
                this.SetStringArrayValue("tags[]", value);
            }
        }

        // This group of tags should be hidden from users and mod developers. Useful for games to
        // tag special functionality, to filter on and use behind the scenes. You can also use
        // Metadata Key Value Pairs for more arbitary data.
        public bool isHidden
        {
            set {
                this.SetStringValue("hidden", value.ToString());
            }
        }
    }
}
