using System;

using Newtonsoft.Json;

namespace ModIO
{
    [System.Serializable]
    public class GameProfile
    {
        /// ---------[ FIELDS ]---------
        /// <summary>
        /// Unique game id.
        /// </summary>
        [JsonProperty("id")]
        public int id;

        /// <summary>
        /// Status of the game (see status and visibility for details):
        /// </summary>
        [JsonProperty("status")]
        public GameStatus status;

        /// <summary>
        /// Name of the game.
        /// </summary>
        [JsonProperty("name")]
        public string name;

        /// <summary>
        /// Subdomain for the game on mod.io.
        /// </summary>
        [JsonProperty("name_id")]
        public string nameId;

        /// <summary>
        /// Summary of the game.
        /// </summary>
        [JsonProperty("summary")]
        public string summary;

        /// <summary>
        /// A guide about creating and uploading mods for this game to mod.io (applicable if
        /// modSubmissionPermission = GameModSubmissionPermission.ToolOnly).
        /// </summary>
        [JsonProperty("instructions")]
        public string instructions;

        /// <summary>
        /// Link to a mod.io guide, your modding wiki or a page where modders can learn how to make
        /// and submit mods to your games profile.
        /// </summary>
        [JsonProperty("instructions_url")]
        public string instructionsURL;

        /// <summary>
        /// Contains user data.
        /// </summary>
        [JsonProperty("submitted_by")]
        public UserProfileStub submittedBy;

        /// <summary>
        /// Unix timestamp of date game was registered.
        /// </summary>
        [JsonProperty("date_added")]
        public int dateAdded;

        /// <summary>
        /// Unix timestamp of date game was updated.
        /// </summary>
        [JsonProperty("date_updated")]
        public int dateUpdated;

        /// <summary>
        /// Unix timestamp of date game was set live.
        /// </summary>
        [JsonProperty("date_live")]
        public int dateLive;

        /// <summary>
        /// Word used to describe user-generated content (mods, items, addons etc).
        /// </summary>
        [JsonProperty("ugc_name")]
        public string ugcName;

        /// <summary>
        /// Presentation style used on the mod.io website:
        /// </summary>
        [JsonProperty("presentation_option")]
        public GameModGalleryPresentation modGalleryPresentation;

        /// <summary>
        /// Submission process modders must follow:
        /// </summary>
        [JsonProperty("submission_option")]
        public GameModSubmissionPermission modSubmissionPermission;

        /// <summary>
        /// Curation process used to approve mods:
        /// </summary>
        [JsonProperty("curation_option")]
        public GameModCuration modCuration;

        /// <summary>
        /// Community features enabled on the mod.io website:
        /// </summary>
        [JsonProperty("community_options")]
        public GameCommunityFeatures communityFeatures;

        /// <summary>
        /// Revenue capabilities mods can enable:
        /// </summary>
        [JsonProperty("revenue_options")]
        public GameModRevenuePermissions modRevenuePermissions;

        /// <summary>
        /// Level of API access allowed by this game:
        /// </summary>
        [JsonProperty("api_access_options")]
        public GameAPIPermissions apiPermissions;

        /// <summary>
        /// Allow developers to select if they flag their mods as containing mature content:
        /// </summary>
        [JsonProperty("maturity_options")]
        public GameModContentPermission contentPermission;


        /// <summary>
        /// Contains icon data.
        /// </summary>
        [JsonProperty("icon")]
        public IconImageLocator iconLocator;

        /// <summary>
        /// Contains logo data.
        /// </summary>
        [JsonProperty("logo")]
        public LogoImageLocator logoLocator;

        /// <summary>
        /// Contains header data.
        /// </summary>
        [JsonProperty("header")]
        public HeaderImageLocator headerImageLocator;

        /// <summary>
        /// URL to the game's mod.io page.
        /// </summary>
        [JsonProperty("profile_url")]
        public string profileURL;

        /// <summary>
        /// Groups of tags configured by the game developer, that mods can select.
        /// </summary>
        [JsonProperty("tag_options")]
        public ModTagCategory[] tagCategories;

    }
}
