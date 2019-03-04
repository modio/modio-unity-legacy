namespace ModIO.API
{
    public static class GetUserRatingsFilterFields
    {
        /// <summary>(int) Unique id of the parent game.</summary>
        public const string gameId = "game_id";
        /// <summary>(int) Unique id of the mod.</summary>
        public const string modId = "mod_id";
        /// <summary>(int) The value of the rating.</summary>
        public const string rating = "rating";
        /// <summary>(int) Unix timestamp of date rating was submitted.</summary>
        public const string dateAdded = "date_added";
    }
}
