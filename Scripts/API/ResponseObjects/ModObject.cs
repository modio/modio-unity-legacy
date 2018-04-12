namespace ModIO.API
{
    [System.Serializable]
    public struct ModObject
    {
        // Unique mod id.
        public int id;
        // Unique game id.
        public int game_id;
        // Status of the mod (see status and visibility for details):
        public int status;
        // Visibility of the mod (see status and visibility for details):
        public int visible;
        // Unix timestamp of date mod was registered.
        public int date_added;
        // Unix timestamp of date mod was updated.
        public int date_updated;
        // Unix timestamp of date mod was set live.
        public int date_live;
        // Official homepage of the mod.
        public string homepage;
        // Name of the mod.
        public string name;
        // Path for the mod on mod.io. For example: https://gamename.mod.io/mod-name-id-here
        public string name_id;
        // Summary of the mod.
        public string summary;
        // Detailed description of the mod which allows HTML.
        public string description;
        // Metadata stored by the game developer. Metadata can also be stored as searchable key value pairs, and to individual mod files.
        public string metadata_blob;
        // URL to the mod's mod.io profile.
        public string profile_url;
        // Contains user data.
        public UserObject submitted_by;
        // Contains logo data.
        public LogoObject logo;
        // Contains modfile data.
        public ModfileObject modfile;
        // Contains mod media data.
        public ModMediaObject media;
        // Contains ratings summary.
        public RatingSummaryObject rating_summary;
        // Contains mod tag data.
        public ModTagObject[] tags;

        public int stock;
    }
}
