namespace ModIO.API
{
    public class AddModKVPMetadataParameters : RequestParameters
    {
        // ---------[ FIELDS ]---------
        /// <summary>
        /// [REQUIRED] Array containing one or more key value pairs
        /// where the the key and value are separated by a colon ':'
        /// (if the string contains multiple colons the split will
        /// occur on the first matched, i.e. pistol-dmg:800:400 will
        /// become key: pistol-dmg, value: 800:400).
        /// </summary>
        public string[] metadata
        {
            set
            {
                this.SetStringArrayValue("metadata[]", value);
            }
        }

        // ---------[ HELPER FUNCTIONS ]---------
        public static string[] ConvertMetadataKVPsToAPIStrings(MetadataKVP[] kvps)
        {
            string[] apiStrings = new string[kvps.Length];
            for(int i = 0; i < kvps.Length; ++i)
            {
                apiStrings[i] = kvps[i].key + ":" + kvps[i].value;
            }
            return apiStrings;
        }

    }
}
