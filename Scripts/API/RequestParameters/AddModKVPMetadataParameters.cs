namespace ModIO.API
{
    public class AddModKVPMetadataParameters : RequestParameters
    {
        // ---------[ FIELDS ]---------
        // [REQUIRED] Array containing one or more key value pairs where the the key and value are separated by a colon ':' (if the string contains multiple colons the split will occur on the first matched, i.e. pistol-dmg:800:400 will become key: pistol-dmg, value: 800:400).
        public string[] metadata
        {
            set
            {
                this.SetStringArrayValue("metadata[]", value);
            }
        }

        // ---------[ CONSTRUCTOR ]---------
        public AddModKVPMetadataParameters(string[] metadataValue)
        {
            this.metadata = metadataValue;
        }
    }
}