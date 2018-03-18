namespace ModIO.API
{
    public class DeleteModTagsParameters : RequestParameters
    {
        // ---------[ FIELDS ]---------
        // An array of tags to delete.
        public string[] tags
        {
            set
            {
                this.SetStringArrayValue("tags", value);
            }
        }

        // ---------[ CONSTRUCTOR ]---------
        public DeleteModTagsParameters(string[] tagsValue)
        {
            this.tags = tagsValue;
        }
    }
}