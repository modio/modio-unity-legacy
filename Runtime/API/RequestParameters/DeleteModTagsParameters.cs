namespace ModIO.API
{
    public class DeleteModTagsParameters : RequestParameters
    {
        // ---------[ FIELDS ]---------
        // An array of tags to delete.
        public string[] tagNames
        {
            set {
                this.SetStringArrayValue("tags[]", value);
            }
        }
    }
}
