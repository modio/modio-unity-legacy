namespace ModIO.API
{
    public static class GetAllGamesFilterFields
    {
        // (integer) Unique id of the game.
        public const string id = "id";
        // (integer) Status of the game (only admins can filter by this field, see status and
        // visibility for details)
        public const string status = "status";
        // (integer) Unique id of the user who has ownership of the game.
        public const string submittedByUserId = "submitted_by";
        // (integer) Unix timestamp of date game was registered.
        public const string dateAdded = "date_added";
        // (integer) Unix timestamp of date game was updated.
        public const string dateUpdated = "date_updated";
        // (integer) Unix timestamp of date game was set live.
        public const string dateLive = "date_live";
        // (string)  Name of the game.
        public const string name = "name";
        // (string)  Subdomain for the game on mod.io.
        public const string nameId = "name_id";
        // (string)  Summary of the game.
        public const string summary = "summary";
        // (string)  Link to a mod.io guide, modding wiki or a page where modders can learn how to
        // make and submit mods.
        public const string instructionsURL = "instructions_url";
        // (string)  Word used to describe user-generated content (mods, items, addons etc).
        public const string ugcName = "ugc_name";
        // (integer) Presentation style used on the mod.io website:
        public const string presentationOption = "presentation_option";
        // (integer) Submission process modders must follow:
        public const string submissionOption = "submission_option";
        // (integer) Curation process used to approve mods:
        public const string curationOption = "curation_option";
        // (integer) Community features enabled on the mod.io website:
        public const string communityOptions = "community_options";
        // (integer) Revenue capabilities mods can enable:
        public const string revenuePermissions = "revenue_options";
        // (integer) Level of API access allowed by this game:
        public const string apiPermissions = "api_access_options";
        // (integer) If the game allows developers to flag mods as containing mature content:
        public const string matureContentPermission = "maturity_options";
    }
}
