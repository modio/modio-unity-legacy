namespace ModIO.API
{
    [System.Serializable]
    public struct GameObject
    {
        // Unique game id.
        public readonly int id;
        // Status of the game (see status and visibility for details):
        public readonly int status;
        // Unix timestamp of date game was registered.
        public readonly int date_added;
        // Unix timestamp of date game was updated.
        public readonly int date_updated;
        // Unix timestamp of date game was set live.
        public readonly int date_live;
        // Presentation style used on the mod.io website:
        public readonly int presentation_option;
        // Submission process modders must follow:
        public readonly int submission_option;
        // Curation process used to approve mods:
        public readonly int curation_option;
        // Community features enabled on the mod.io website:
        public readonly int community_options;
        // Revenue capabilities mods can enable:
        public readonly int revenue_options;
        // Level of API access allowed by this game:
        public readonly int api_access_options;
        // Word used to describe user-generated content (mods, items, addons etc).
        public readonly string ugc_name;
        // Official homepage of the game.
        public readonly string homepage;
        // Name of the game.
        public readonly string name;
        // Subdomain for the game on mod.io.
        public readonly string name_id;
        // Summary of the game.
        public readonly string summary;
        // A guide about creating and uploading mods for this game to mod.io (applicable if submission_option = 0).
        public readonly string instructions;
        // URL to the game's mod.io page.
        public readonly string profile_url;
        // Contains user data.
        public readonly UserObject submitted_by;
        // Contains icon data.
        public readonly IconObject icon;
        // Contains logo data.
        public readonly LogoObject logo;
        // Contains header data.
        public readonly HeaderImageObject header;
        // Groups of tags configured by the game developer, that mods can select.
        public readonly GameTagOptionObject[] tag_options;
    }
}