namespace ModIO.API
{
    [System.Serializable]
    public struct ModTagObject
    {
        // Tag name.
        public readonly string name;
        // Unix timestamp of date tag was applied.
        public readonly int date_added;
    }
}