namespace ModIO.API
{
    [System.Serializable]
    public struct ModTagObject
    {
        // Tag name.
        public string name;
        // Unix timestamp of date tag was applied.
        public int date_added;
    }
}