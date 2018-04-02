namespace ModIO.API
{
    [System.Serializable]
    public struct ModObject
    {
        // Unique mod id.
        public readonly int id;
        // Unique game id.
        public readonly int game_id;
        // Status of the mod (see status and visibility for details):
        public readonly int status;
        // Visibility of the mod (see status and visibility for details):
        public readonly int visible;
        // Unix timestamp of date mod was registered.
        public readonly int date_added;
        // Unix timestamp of date mod was updated.
        public readonly int date_updated;
        // Unix timestamp of date mod was set live.
        public readonly int date_live;
        // Official homepage of the mod.
        public readonly string homepage;
        // Name of the mod.
        public readonly string name;
        // Path for the mod on mod.io. For example: https://gamename.mod.io/mod-name-id-here
        public readonly string name_id;
        // Summary of the mod.
        public readonly string summary;
        // Detailed description of the mod which allows HTML.
        public readonly string description;
        // Metadata stored by the game developer. Metadata can also be stored as searchable key value pairs, and to individual mod files.
        public readonly string metadata_blob;
        // URL to the mod's mod.io profile.
        public readonly string profile_url;
        // Contains user data.
        public readonly UserObject submitted_by;
        // Contains logo data.
        public readonly LogoObject logo;
        // Contains modfile data.
        public readonly ModfileObject modfile;
        // Contains mod media data.
        public readonly ModMediaObject media;
        // Contains ratings summary.
        public readonly RatingSummaryObject rating_summary;
        // Contains mod tag data.
        public readonly ModTagObject[] tags;

        public int stock;
    }
}
