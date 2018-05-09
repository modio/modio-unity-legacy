using System;

using Newtonsoft.Json;

namespace ModIO
{
    public enum GameStatus
    {
        NotAccepted = 0,
        Accepted = 1,
        Archived = 2,
        Deleted = 3,
    }

    public enum ModGalleryPresentationOption
    {
        GridView = 0,
        TableView = 1,
    }

    public enum ModSubmissionOption
    {
        // Mod uploads must occur via a tool created by the game developers
        ToolOnly = 0,
        // Mod uploads can occur from anywhere, including the website and API
        Unrestricted = 1,
    }

    public enum ModCurationOption
    {
        // Mods are immediately available to play
        None = 0,
        // Mods are immediately available to play unless they choose to receive donations.
        // These mods must be accepted to be listed
        Paid = 1,
        // All mods must be accepted by someone to be listed
        Full = 2,
    }

    [Flags]
    public enum GameCommunityOptions
    {
        // All of the options below are disabled
        Disabled = 0,
        // Discussion board enabled
        DiscussionBoard = 0x01,
        // Guides and news enabled
        GuidesAndNews = 0x02,
    }

    [Flags]
    public enum ModRevenuePermissions
    {
        // All of the options below are disabled
        None = 0,
        // Allow mods to be sold
        AllowSales = 0x01,
        // Allow mods to receive donations
        AllowDonations = 0x02,
        // Allow mods to be traded
        AllowModTrading = 0x04,
        // Allow mods to control supply and scarcity
        AllowModScarcity = 0x08,
    }

    [Flags]
    public enum GameAPIPermissions
    {
        // All of the options below are disabled
        RestrictAll = 0,
        // Allow 3rd parties to access this games API endpoints
        AllowPublicAccess = 1,
        // Allow mods to be downloaded directly
        // (If disabled all download URLs will contain a frequently
        // changing verification hash to stop unauthorized use)
        AllowDirectDownloads = 2,
    }

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
        /// Presentation style used on the mod.io website:
        /// </summary>
        [JsonProperty("presentation_option")]
        public ModGalleryPresentationOption presentationOption;

        /// <summary>
        /// Submission process modders must follow:
        /// </summary>
        [JsonProperty("submission_option")]
        public ModSubmissionOption submissionOption;

        /// <summary>
        /// Curation process used to approve mods:
        /// </summary>
        [JsonProperty("curation_option")]
        public ModCurationOption curationOption;

        /// <summary>
        /// Community features enabled on the mod.io website:
        /// </summary>
        [JsonProperty("community_options")]
        public GameCommunityOptions communityOptions;

        /// <summary>
        /// Revenue capabilities mods can enable:
        /// </summary>
        [JsonProperty("revenue_options")]
        public ModRevenuePermissions revenuePermissions;

        /// <summary>
        /// Level of API access allowed by this game:
        /// </summary>
        [JsonProperty("api_access_options")]
        public GameAPIPermissions apiPermissions;

        /// <summary>
        /// Word used to describe user-generated content (mods, items, addons etc).
        /// </summary>
        [JsonProperty("ugc_name")]
        public string ugcName;

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
        /// A guide about creating and uploading mods for this game to mod.io (applicable if submissionOption = 0).
        /// </summary>
        [JsonProperty("instructions")]
        public string instructions;

        /// <summary>
        /// Link to a mod.io guide, your modding wiki or a page where modders can learn how to make and submit mods to your games profile.
        /// </summary>
        [JsonProperty("instructions_url")]
        public string instructionsURL;

        /// <summary>
        /// URL to the game's mod.io page.
        /// </summary>
        [JsonProperty("profile_url")]
        public string profileURL;

        /// <summary>
        /// Groups of tags configured by the game developer, that mods can select.
        /// </summary>
        [JsonProperty("tag_options")]
        public ModTagCategory[] taggingOptions;

    }
}