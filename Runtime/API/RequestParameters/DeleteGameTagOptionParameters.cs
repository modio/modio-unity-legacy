namespace ModIO.API
{
    public class DeleteGameTagOptionParameters : RequestParameters
    {
        // ---------[ FIELDS ]---------
        // Name of the tag group that you want to delete tags from.
        public string name
        {
            set {
                this.SetStringValue("name", value);
            }
        }

        // Array of strings representing the tag options to delete. An empty array will delete the
        // entire group.
        public string[] tags
        {
            set {
                this.SetStringArrayValue("tags[]", value);
            }
        }
    }
}
