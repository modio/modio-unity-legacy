namespace ModIO.API
{
    public class AddModTagsParameters : RequestParameters
    {
        // ---------[ FIELDS ]---------
        // [REQUIRED] An array of tags to add. For example: If the parent game has a 'Theme' tag
        // group with 'Fantasy', 'Sci-fi', 'Western' and 'Realistic' as the options, you could add
        // 'Fantasy' and 'Sci-fi' to the tags array in your request. Provided the tags are valid you
        // can add any number.
        public string[] tagNames
        {
            set {
                this.SetStringArrayValue("tags[]", value);
            }
        }
    }
}
