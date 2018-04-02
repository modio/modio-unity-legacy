namespace ModIO.API
{
    [System.Serializable]
    public struct ModDependenciesObject
    {
        // Unique id of the mod that is the dependency.
        public readonly int mod_id;
        // Unix timestamp of date the dependency was added.
        public readonly int date_added;
    }
}