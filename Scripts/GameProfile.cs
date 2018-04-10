using System;
using System.Collections.Generic;

using SerializeField = UnityEngine.SerializeField;

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

    [Serializable]
    public class GameProfile
    {
        // ---------[ INNER CLASSES ]---------
        [System.Serializable]
        public class LogoImageLocator : MultiVersionImageLocator<GameLogoVersion>
        {
            // ---------[ ABSTRACTS ]---------
            protected override GameLogoVersion FullSizeVersionEnum() { return GameLogoVersion.FullSize; }

            // ---------[ API OBJECT INTERFACE ]---------
            public void ApplyLogoObjectValues(API.LogoObject apiObject)
            {
                this._fileName = apiObject.filename;
                this._versionPairing = new VersionSourcePair[]
                {
                    new VersionSourcePair()
                    {
                        version = GameLogoVersion.FullSize,
                        source = apiObject.original
                    },
                    new VersionSourcePair()
                    {
                        version = GameLogoVersion.Thumbnail_320x180,
                        source = apiObject.thumb_320x180
                    },
                    new VersionSourcePair()
                    {
                        version = GameLogoVersion.Thumbnail_640x360,
                        source = apiObject.thumb_640x360
                    },
                    new VersionSourcePair()
                    {
                        version = GameLogoVersion.Thumbnail_1280x720,
                        source = apiObject.thumb_1280x720
                    },
                };
            }

            public static LogoImageLocator CreateFromLogoObject(API.LogoObject apiObject)
            {
                var retVal = new LogoImageLocator();
                retVal.ApplyLogoObjectValues(apiObject);
                return retVal;
            }
        }

        // ---------[ SERIALIZED MEMBERS ]---------
        [SerializeField] private int _id;
        [SerializeField] private GameStatus _status;
        [SerializeField] private int _submittedById;
        [SerializeField] private TimeStamp _dateAdded;
        [SerializeField] private TimeStamp _dateUpdated;
        [SerializeField] private TimeStamp _dateLive;
        [SerializeField] private ModGalleryPresentationOption _presentationOption;
        [SerializeField] private ModSubmissionOption _submissionOption;
        [SerializeField] private ModCurationOption _curationOption;
        [SerializeField] private GameCommunityOptions _communityOptions;
        [SerializeField] private ModRevenuePermissions _revenuePermissions;
        [SerializeField] private GameAPIPermissions _apiPermissions;
        [SerializeField] private string _ugcName;
        [SerializeField] private IconImageSet _icon;
        [SerializeField] private LogoImageLocator _logo;
        [SerializeField] private HeaderImageSet _headerImage;
        [SerializeField] private string _homepageURL;
        [SerializeField] private string _name;
        [SerializeField] private string _nameId;
        [SerializeField] private string _summary;
        [SerializeField] private string _instructions;
        [SerializeField] private string _profileURL;
        [SerializeField] private ModTagCategory[] _taggingOptions;

        // ---------[ FIELDS ]---------
        public int id                                           { get { return this._id; } }
        public GameStatus status                                { get { return this._status; } }
        public int submittedById                                { get { return this._submittedById; } }
        public TimeStamp dateAdded                              { get { return this._dateAdded; } }
        public TimeStamp dateUpdated                            { get { return this._dateUpdated; } }
        public TimeStamp dateLive                               { get { return this._dateLive; } }
        public ModGalleryPresentationOption presentationOption  { get { return this._presentationOption; } }
        public ModSubmissionOption submissionOption             { get { return this._submissionOption; } }
        public ModCurationOption curationOption                 { get { return this._curationOption; } }
        public GameCommunityOptions communityOptions            { get { return this._communityOptions; } }
        public ModRevenuePermissions revenuePermissions         { get { return this._revenuePermissions; } }
        public GameAPIPermissions apiPermissions                { get { return this._apiPermissions; } }
        public string ugcName                                   { get { return this._ugcName; } }
        public IconImageSet icon                               { get { return this._icon; } }
        public LogoImageLocator logo                               { get { return this._logo; } }
        public HeaderImageSet headerImage                      { get { return this._headerImage; } }
        public string homepageURL                               { get { return this._homepageURL; } }
        public string name                                      { get { return this._name; } }
        public string nameId                                    { get { return this._nameId; } }
        public string summary                                   { get { return this._summary; } }
        public string instructions                              { get { return this._instructions; } }
        public string profileURL                                { get { return this._profileURL; } }
        public ICollection<ModTagCategory> taggingOptions       { get { return new List<ModTagCategory>(this.taggingOptions); } }
        
        // ---------[ API OBJECT INTERFACE ]---------
        public void ApplyGameObjectValues(API.GameObject apiObject)
        {
            this._id = apiObject.id;
            this._status = (GameStatus)apiObject.status;
            this._submittedById = apiObject.submitted_by.id;
            this._dateAdded = TimeStamp.GenerateFromServerTimeStamp(apiObject.date_added);
            this._dateUpdated = TimeStamp.GenerateFromServerTimeStamp(apiObject.date_updated);
            this._dateLive = TimeStamp.GenerateFromServerTimeStamp(apiObject.date_live);
            this._presentationOption = (ModGalleryPresentationOption)apiObject.presentation_option;
            this._submissionOption = (ModSubmissionOption)apiObject.submission_option;
            this._curationOption = (ModCurationOption)apiObject.curation_option;
            this._communityOptions = (GameCommunityOptions)apiObject.community_options;
            this._revenuePermissions = (ModRevenuePermissions)apiObject.revenue_options;
            this._apiPermissions = (GameAPIPermissions)apiObject.api_access_options;
            this._ugcName = apiObject.ugc_name;
            this._icon = IconImageSet.CreateFromIconObject(apiObject.icon);
            this._logo = LogoImageLocator.CreateFromLogoObject(apiObject.logo);
            this._headerImage = HeaderImageSet.CreateFromHeaderImageObject(apiObject.header);
            this._homepageURL = apiObject.homepage;
            this._name = apiObject.name;
            this._nameId = apiObject.name_id;
            this._summary = apiObject.summary;
            this._instructions = apiObject.instructions;
            this._profileURL = apiObject.profile_url;

            this._taggingOptions = new ModTagCategory[apiObject.tag_options.Length];
            for(int i = 0; i < apiObject.tag_options.Length; ++i)
            {
                this._taggingOptions[i] = ModTagCategory.CreateFromGameTagOptionObject(apiObject.tag_options[i]);
            }
        }

        public static GameProfile CreateFromGameObject(API.GameObject apiObject)
        {
            var newGP = new GameProfile();
            newGP.ApplyGameObjectValues(apiObject);
            return newGP;
        }
    }
}
