namespace ModIO.API
{
    public static class GetModTagsFilterFields
    {
        // (integer) Unix timestamp of date tag was added.
        public const string dateAdded = "date_added";

        // (string)  String representation of the tag. You can check the eligible tags on the parent
        // game object to determine all possible values for this field.
        public const string tagName = "tag";
    }
}
