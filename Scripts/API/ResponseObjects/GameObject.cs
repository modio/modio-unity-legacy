namespace ModIO.API
{
    [System.Serializable]
    public struct GameObject
    {
        // Unique game id.
        public int id;
        // Status of the game (see status and visibility for details):
        public int status;
        // Unix timestamp of date game was registered.
        public int date_added;
        // Unix timestamp of date game was updated.
        public int date_updated;
        // Unix timestamp of date game was set live.
        public int date_live;
        // Presentation style used on the mod.io website:
        public int presentation_option;
        // Submission process modders must follow:
        public int submission_option;
        // Curation process used to approve mods:
        public int curation_option;
        // Community features enabled on the mod.io website:
        public int community_options;
        // Revenue capabilities mods can enable:
        public int revenue_options;
        // Level of API access allowed by this game:
        public int api_access_options;
        // Word used to describe user-generated content (mods, items, addons etc).
        public string ugc_name;
        // Name of the game.
        public string name;
        // Subdomain for the game on mod.io.
        public string name_id;
        // Summary of the game.
        public string summary;
        // A guide about creating and uploading mods for this game to mod.io (applicable if submission_option = 0).
        public string instructions;
        // Link to a mod.io guide, your modding wiki or a page where modders can learn how to make and submit mods to your games profile.
        public string instructions_url;
        // URL to the game's mod.io page.
        public string profile_url;
        // Contains user data.
        public UserObject submitted_by;
        // Contains icon data.
        public IconImageLocator icon;
        // Contains logo data.
        public LogoImageLocator logo;
        // Contains header data.
        public HeaderImageLocator header;
        // Groups of tags configured by the game developer, that mods can select.
        public GameTagOptionObject[] tag_options;
    }
}