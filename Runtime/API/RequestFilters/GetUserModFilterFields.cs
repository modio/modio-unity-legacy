namespace ModIO.API
{
    public static class GetUserModFilterFields
    {
        // (integer) Unique id of the mod.
        public const string id = "id";
        // (integer) Unique id of the parent game.
        public const string gameId = "game_id";
        // (integer) Status of the mod (only game admins can filter by this field, see status and
        // visibility for details):
        public const string status = "status";
        // (integer) Visibility of the mod (only game admins can filter by this field, see status
        // and visibility for details):
        public const string visibility = "visible";
        // (integer) Unique id of the user who has ownership of the mod.
        public const string submittedByUserId = "submitted_by";
        // (integer) Unix timestamp of date mod was registered.
        public const string dateAdded = "date_added";
        // (integer) Unix timestamp of date mod was updated.
        public const string dateUpdated = "date_updated";
        // (integer) Unix timestamp of date mod was set live.
        public const string dateLive = "date_live";
        // (string)  Name of the mod.
        public const string name = "name";
        // (string)  Path for the mod on mod.io. For example:
        // https://gamename.mod.io/mod-name-id-here
        public const string nameId = "name_id";
        // (string)  Summary of the mod.
        public const string summary = "summary";
        // (string)  Detailed description of the mod which allows HTML.
        public const string description = "description";
        // (string)  Official homepage of the mod.
        public const string homepageURL = "homepage_url";
        // (integer) Unique id of the file that is the current active release.
        public const string modfileId = "modfile";
        // (string)  Metadata stored by the game developer.
        public const string metadataBlob = "metadata_blob";
        // (string)  Colon-separated values representing the key-value pairs you want to filter the
        // results by. If you supply more than one key-pair, separate the pairs by a comma. Will
        // only filter by an exact key-pair match.
        public const string metadataKVP = "metadata_kvp";
        // (string)  Comma-separated values representing the tags you want to filter the results by.
        // Only tags that are supported by the parent game can be applied. To determine what tags
        // are eligible, see the tags values within tag_options column on the parent Game Object.
        public const string tagNames = "tags";
        // (string)  Sort results by most downloads using _sort filter parameter, value should be
        // downloads for descending or -downloads for ascending results.
        public const string downloads = "downloads";
        // (string)  Sort results by popularity using _sort filter, value should be popular for
        // descending or -popular for ascending results.
        public const string popular = "popular";
        // (string)  Sort results by weighted rating using _sort filter, value should be rating for
        // descending or -rating for ascending results.
        public const string rating = "rating";
        // (string)  Sort results by most subscribers using _sort filter, value should be
        // subscribers for descending or -subscribers for ascending results.
        public const string subscribers = "subscribers";
    }
}
