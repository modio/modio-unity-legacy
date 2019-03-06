namespace ModIO.API
{
    public static class GetAllModStatsFilterFields
    {
        // (integer) Unique id of the mod.
        public const string modId = "mod_id";
        // (integer) Current ranking by popularity for the corresponding mod.
        public const string popularityRankPosition = "popularity_rank_position";
        // (integer) Global mod count in which popularityRankPosition is compared against.
        public const string popularityRankTotalMods = "popularity_rank_total_mods";
        // (integer) A sum of all modfile downloads for the corresponding mod.
        public const string downloadsTotal = "downloads_total";
        // (integer) A sum of all current subscribers for the corresponding mod.
        public const string subscribersTotal = "subscribers_total";
        // (integer) Amount of positive ratings.
        public const string ratingsPositive = "ratings_positive";
        // (integer) Amount of negative ratings.
        public const string ratingsNegative = "ratings_negative";
    }
}
