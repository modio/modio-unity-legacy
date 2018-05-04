using System;
using System.Collections.Generic;

using SerializeField = UnityEngine.SerializeField;

using Newtonsoft.Json;

namespace ModIO
{
    // - Enums -
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
        ToolOnly = 0,
        Unrestricted = 1,
    }

    public enum ModCurationOption
    {
        None = 0,
        Paid = 1,
        Full = 2,
    }

    [Flags]
    public enum GameCommunityOptions
    {
        Disabled = 0,
        DiscussionBoard = 0x01,
        GuidesAndNews = 0x02,
    }

    [Flags]
    public enum ModRevenuePermissions
    {
        None = 0,
        AllowSales = 0x01,
        AllowDonations = 0x02,
        AllowModTrading = 0x04,
        AllowModScarcity = 0x08,
    }

    [Flags]
    public enum GameAPIPermissions
    {
        RestrictAll = 0,
        AllowPublicAccess = 1, // This game allows 3rd parties to access the mods API
        AllowDirectDownloads = 2, // This game allows mods to be downloaded directly without API validation
    }

    public enum GameLogoVersion
    {
        FullSize = 0,
        Thumbnail_320x180,
        Thumbnail_640x360,
        Thumbnail_1280x720,
    }

    public enum GameIconVersion
    {
        FullSize = 0,
        Thumbnail_64x64,
        Thumbnail_128x128,
        Thumbnail_256x256,
    }

    [Serializable]
    public class GameProfile
    {
        // ---------[ SERIALIZED MEMBERS ]---------
        [JsonProperty] private int _id;
        [JsonProperty] private GameStatus _status;
        [JsonProperty] private int _submittedById;
        [JsonProperty] private int _dateAdded;
        [JsonProperty] private int _dateUpdated;
        [JsonProperty] private int _dateLive;
        [JsonProperty] private ModGalleryPresentationOption _presentationOption;
        [JsonProperty] private ModSubmissionOption _submissionOption;
        [JsonProperty] private ModCurationOption _curationOption;
        [JsonProperty] private GameCommunityOptions _communityOptions;
        [JsonProperty] private ModRevenuePermissions _revenuePermissions;
        [JsonProperty] private GameAPIPermissions _apiPermissions;
        [JsonProperty] private string _ugcName;
        [JsonProperty] private IconImageLocator _iconLocator;
        [JsonProperty] private LogoImageLocator _logoLocator;
        [JsonProperty] private HeaderImageLocator _headerImageLocator;
        [JsonProperty] private string _instructionsURL;
        [JsonProperty] private string _name;
        [JsonProperty] private string _nameId;
        [JsonProperty] private string _summary;
        [JsonProperty] private string _instructions;
        [JsonProperty] private string _profileURL;
        [JsonProperty] private ModTagCategory[] _taggingOptions;

        // ---------[ FIELDS ]---------
        [JsonIgnore] public int id                                           { get { return this._id; } }
        [JsonIgnore] public GameStatus status                                { get { return this._status; } }
        [JsonIgnore] public int submittedById                                { get { return this._submittedById; } }
        [JsonIgnore] public int dateAdded                              { get { return this._dateAdded; } }
        [JsonIgnore] public int dateUpdated                            { get { return this._dateUpdated; } }
        [JsonIgnore] public int dateLive                               { get { return this._dateLive; } }
        [JsonIgnore] public ModGalleryPresentationOption presentationOption  { get { return this._presentationOption; } }
        [JsonIgnore] public ModSubmissionOption submissionOption             { get { return this._submissionOption; } }
        [JsonIgnore] public ModCurationOption curationOption                 { get { return this._curationOption; } }
        [JsonIgnore] public GameCommunityOptions communityOptions            { get { return this._communityOptions; } }
        [JsonIgnore] public ModRevenuePermissions revenuePermissions         { get { return this._revenuePermissions; } }
        [JsonIgnore] public GameAPIPermissions apiPermissions                { get { return this._apiPermissions; } }
        [JsonIgnore] public string ugcName                                   { get { return this._ugcName; } }
        [JsonIgnore] public IconImageLocator iconLocator                     { get { return this._iconLocator; } }
        [JsonIgnore] public LogoImageLocator logoLocator                     { get { return this._logoLocator; } }
        [JsonIgnore] public HeaderImageLocator headerImageLocator            { get { return this._headerImageLocator; } }
        [JsonIgnore] public string name                                      { get { return this._name; } }
        [JsonIgnore] public string nameId                                    { get { return this._nameId; } }
        [JsonIgnore] public string summary                                   { get { return this._summary; } }
        [JsonIgnore] public string instructions                              { get { return this._instructions; } }
        [JsonIgnore] public string instructionsURL                           { get { return this._instructionsURL; } }
        [JsonIgnore] public string profileURL                                { get { return this._profileURL; } }
        [JsonIgnore] public ICollection<ModTagCategory> taggingOptions       { get { return new List<ModTagCategory>(this._taggingOptions); } }
        
        // ---------[ API OBJECT INTERFACE ]---------
        public void ApplyGameObjectValues(API.GameObject apiObject)
        {
            this._id = apiObject.id;
            this._status = (GameStatus)apiObject.status;
            this._submittedById = apiObject.submitted_by.id;
            this._dateAdded = apiObject.date_added;
            this._dateUpdated = apiObject.date_updated;
            this._dateLive = apiObject.date_live;
            this._presentationOption = (ModGalleryPresentationOption)apiObject.presentation_option;
            this._submissionOption = (ModSubmissionOption)apiObject.submission_option;
            this._curationOption = (ModCurationOption)apiObject.curation_option;
            this._communityOptions = (GameCommunityOptions)apiObject.community_options;
            this._revenuePermissions = (ModRevenuePermissions)apiObject.revenue_options;
            this._apiPermissions = (GameAPIPermissions)apiObject.api_access_options;
            this._ugcName = apiObject.ugc_name;
            this._iconLocator = apiObject.icon;
            this._logoLocator = apiObject.logo;
            this._headerImageLocator = apiObject.header;
            this._instructionsURL = apiObject.instructions_url;
            this._name = apiObject.name;
            this._nameId = apiObject.name_id;
            this._summary = apiObject.summary;
            this._instructions = apiObject.instructions;
            this._profileURL = apiObject.profile_url;

            Utility.SafeMapArraysOrZero(apiObject.tag_options,
                                        (o) => { return ModTagCategory.CreateFromGameTagOptionObject(o); },
                                        out this._taggingOptions);
        }

        public static GameProfile CreateFromGameObject(API.GameObject apiObject)
        {
            var newGP = new GameProfile();
            newGP.ApplyGameObjectValues(apiObject);
            return newGP;
        }
    }
}
