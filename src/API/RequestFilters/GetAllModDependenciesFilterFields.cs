namespace ModIO.API
{
    public static class GetAllModDependenciesFilterFields
    {
        // (integer) Unique id of the mod that is the dependency.
        public const string modId = "mod_id";
        // (integer) Unix timestamp of date the dependency was added.
        public const string dateAdded = "date_added";
    }
}
