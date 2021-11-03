namespace ModIO.API
{
    public class DeleteModKVPMetadataParameters : RequestParameters
    {
        // ---------[ FIELDS ]---------
        /// <summary>
        /// [REQUIRED] Array containing one or more metadata keys to be removed from the
        /// [[ModIO.ModProfile]].
        /// </summary>
        public string[] metadataKeys
        {
            set {
                this.SetStringArrayValue("metadata[]", value);
            }
        }
    }
}
