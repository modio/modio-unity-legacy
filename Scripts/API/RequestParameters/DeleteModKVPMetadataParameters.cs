namespace ModIO.API
{
    public class DeleteModKVPMetadataParameters : RequestParameters
    {
        // ---------[ FIELDS ]---------
        /// <summary>
        /// [REQUIRED] Array containing one or more key value pairs to
        /// delete where the the key and value are separated by a
        /// colon ':'.
        /// </summary>
        /// <remark>
        /// If an array value contains only the key and no colon ':',
        /// all metadata with that key will be removed.
        /// </remark>
        public string[] metadataKeys
        {
            set
            {
                this.SetStringArrayValue("metadata[]", value);
            }
        }
    }
}
