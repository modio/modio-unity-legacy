namespace ModIO.API
{
    [System.Serializable]
    public struct ModObject
    {
        // - Value Interpretation -
        public static class StatusValue
        {
            public const int NotAccepted = 0;
            public const int Accepted = 1;
            public const int Archived = 2;
            public const int Deleted = 3;
        }

        public static class VisibleValue
        {
            public const int Hidden = 0;
            public const int Public = 1;
        }

        /// <summary> Unique mod id. </summary>
        public int id;

        /// <summary> Unique game id. </summary>
        public int game_id;

        /// <summary> Status of the mod.
        /// See <see cref="ModIO.API.ModObject.StatusValues"/> for possible values.
        /// <a href="https://docs.mod.io/#status-amp-visibility">Status and Visibility Documentation</a>
        /// </summary>
        public int status;

        /// <summary> Visibility of the mod.
        /// See <see cref="ModIO.API.ModObject.VisibleValues"/> for possible values.
        /// <a href="https://docs.mod.io/#status-amp-visibility">Status and Visibility Documentation</a>
        /// </summary>
        public int visible;

        /// <summary> Contains user data. </summary>
        public UserObject submitted_by;

        /// <summary> Unix timestamp of date mod was registered. </summary>
        public int date_added;

        /// <summary> Unix timestamp of date mod was updated. </summary>
        public int date_updated;

        /// <summary> Unix timestamp of date mod was set live. </summary>
        public int date_live;

        /// <summary> Contains logo data. </summary>
        public LogoObject logo;

        /// <summary> Official homepage of the mod. </summary>
        public string homepage_url;

        /// <summary> Name of the mod. </summary>
        public string name;

        /// <summary> Path for the mod on mod.io.
        /// For example: https://gamename.mod.io/mod-name-id-here
        /// </summary>
        public string name_id;

        /// <summary> Summary of the mod. </summary>
        public string summary;

        /// <summary> Detailed description of the mod which allows HTML. </summary>
        public string description;

        /// <summary> Metadata stored by the game developer.
        /// Metadata can also be stored as searchable key value pairs,
        /// and to individual mod files.
        /// </summary>
        public string metadata_blob;

        /// <summary> URL to the mod's mod.io profile. </summary>
        public string profile_url;

        /// <summary> Contains modfile data. </summary>
        public ModfileObject modfile;

        /// <summary> Contains mod media data. </summary>
        public ModMediaObject media;

        /// <summary> Contains ratings summary. </summary>
        public RatingSummaryObject rating_summary;
        
        /// <summary> Contains key-value metadata. </summary>
        public MetadataKVPObject[] metadata_kvp;
        
        /// <summary> Contains mod tag data. </summary>
        public ModTagObject[] tags;
    }
}
