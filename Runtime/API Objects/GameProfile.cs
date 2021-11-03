using Newtonsoft.Json;

namespace ModIO
{
    [System.Serializable]
    public class GameProfile
    {
        // ---------[ CONSTANTS ]---------
        /// <summary>An id value indicating an invalid profile.</summary>
        public const int NULL_ID = 0;

        // ---------[ FIELDS ]---------
        /// <summary>mod.io id of the game profile.</summary>
        [JsonProperty("id")]
        public int id;

        /// <summary>Status of the game profile on mod.io.</summary>
        [JsonProperty("status")]
        public GameStatus status;

        /// <summary>Name of the game.</summary>
        [JsonProperty("name")]
        public string name;

        /// <summary>Unique name identifier on mod.io.</summary>
        [JsonProperty("name_id")]
        public string nameId;

        /// <summary>A brief description of the game.</summary>
        [JsonProperty("summary")]
        public string summary;

        /// <summary>The instructions for uploading mods presented on the mod.io website.</summary>
        [JsonProperty("instructions")]
        public string instructions;

        /// <summary>Link to the instructions for uploading mods.</summary>
        [JsonProperty("instructions_url")]
        public string instructionsURL;

        /// <summary>User that submitted the game profile to mod.io.</summary>
        [JsonProperty("submitted_by")]
        public UserProfile submittedBy;

        /// <summary>Server time stamp for the creation of the game profile.</summary>
        [JsonProperty("date_added")]
        public int dateAdded;

        /// <summary>Server time stamp of last edit made to the game profile.</summary>
        [JsonProperty("date_updated")]
        public int dateUpdated;

        /// <summary>Server time stamp for the first time the game profile was made live.</summary>
        [JsonProperty("date_live")]
        public int dateLive;

        /// <summary>Word used to describe the user-generated content on mod.io.</summary>
        [JsonProperty("ugc_name")]
        public string ugcName;

        /// <summary>Default mod gallery presentation on the mod.io website.</summary>
        [JsonProperty("presentation_option")]
        public GameModGalleryPresentation modGalleryPresentation;

        /// <summary>Permissable method of mod uploading for this game to the mod.io
        /// website.</summary>
        [JsonProperty("submission_option")]
        public GameModSubmissionPermission modSubmissionPermission;

        /// <summary>Curation process for uploaded mods.</summary>
        [JsonProperty("curation_option")]
        public GameModCuration modCuration;

        /// <summary>Community features enabled on the mod.io website.</summary>
        [JsonProperty("community_options")]
        public GameCommunityFeatures communityFeatures;

        /// <summary>Permissable revenue options for uploaded mods.</summary>
        [JsonProperty("revenue_options")]
        public GameModRevenuePermissions modRevenuePermissions;

        /// <summary>Permissions of queries made to endpoints belonging to this game.</summary>
        [JsonProperty("api_access_options")]
        public GameAPIPermissions apiPermissions;

        /// <summary>Mod content that this game permits.</summary>
        [JsonProperty("maturity_options")]
        public GameModContentPermission contentPermission;

        /// <summary>Icon locator for the game's website profile.</summary>
        [JsonProperty("icon")]
        public IconImageLocator iconLocator;

        /// <summary>Logo logocator for the game's website profile.</summary>
        [JsonProperty("logo")]
        public LogoImageLocator logoLocator;

        /// <summary>Header image locator for the game's website profile.</summary>
        [JsonProperty("header")]
        public HeaderImageLocator headerImageLocator;

        /// <summary>URL for the game's website profile.</summary>
        [JsonProperty("profile_url")]
        public string profileURL;

        /// <summary>Tagging categories this game offers to mods.</summary>
        [JsonProperty("tag_options")]
        public ModTagCategory[] tagCategories;
    }
}
