namespace ModIO.API
{
    [System.Serializable]
    public struct MetadataKVPObject
    {
        // The key of the key-value pair.
        public readonly string metakey;
        // The value of the key-value pair.
        public readonly string metavalue;
    }
}