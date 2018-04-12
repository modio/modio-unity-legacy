namespace ModIO.API
{
    [System.Serializable]
    public struct ModDependenciesObject
    {
        // Unique id of the mod that is the dependency.
        public int mod_id;
        // Unix timestamp of date the dependency was added.
        public int date_added;
    }
}