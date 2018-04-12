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

        [System.Serializable]
        public class HeaderImageLocator : SingleVersionImageLocator
        {
            // ---------[ API OBJECT INTERFACE ]---------
            public void ApplyHeaderImageObjectValues(API.HeaderImageObject apiObject)
            {
                this._fileName = apiObject.filename;
                this._source = apiObject.original;
            }

            public static HeaderImageLocator CreateFromHeaderImageObject(API.HeaderImageObject apiObject)
            {
                var retVal = new HeaderImageLocator();
                retVal.ApplyHeaderImageObjectValues(apiObject);
                return retVal;
            }
        }

        [System.Serializable]
        public class IconImageLocator : MultiVersionImageLocator<GameIconVersion>
        {
            // ---------[ ABSTRACTS ]---------
            protected override GameIconVersion FullSizeVersionEnum() { return GameIconVersion.FullSize; }

            // ---------[ API OBJECT INTERFACE ]---------
            public void ApplyIconObjectValues(API.IconObject apiObject)
            {
                this._fileName = apiObject.filename;
                this._versionPairing = new VersionSourcePair[]
                {
                    new VersionSourcePair()
                    {
                        version = GameIconVersion.FullSize,
                        source = apiObject.original
                    },
                    new VersionSourcePair()
                    {
                        version = GameIconVersion.Thumbnail_64x64,
                        source = apiObject.thumb_64x64
                    },
                    new VersionSourcePair()
                    {
                        version = GameIconVersion.Thumbnail_128x128,
                        source = apiObject.thumb_128x128
                    },
                    new VersionSourcePair()
                    {
                        version = GameIconVersion.Thumbnail_256x256,
                        source = apiObject.thumb_256x256
                    },
                };
            }

            public static IconImageLocator CreateFromIconObject(API.IconObject apiObject)
            {
                var retVal = new IconImageLocator();
                retVal.ApplyIconObjectValues(apiObject);
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
        [SerializeField] private IconImageLocator _iconLocator;
        [SerializeField] private LogoImageLocator _logoLocator;
        [SerializeField] private HeaderImageLocator _headerImageLocator;
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
        public IconImageLocator iconLocator                     { get { return this._iconLocator; } }
        public LogoImageLocator logoLocator                     { get { return this._logoLocator; } }
        public HeaderImageLocator headerImageLocator            { get { return this._headerImageLocator; } }
        public string homepageURL                               { get { return this._homepageURL; } }
        public string name                                      { get { return this._name; } }
        public string nameId                                    { get { return this._nameId; } }
        public string summary                                   { get { return this._summary; } }
        public string instructions                              { get { return this._instructions; } }
        public string profileURL                                { get { return this._profileURL; } }
        public ICollection<ModTagCategory> taggingOptions       { get { return new List<ModTagCategory>(this._taggingOptions); } }
        
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
            this._iconLocator = IconImageLocator.CreateFromIconObject(apiObject.icon);
            this._logoLocator = LogoImageLocator.CreateFromLogoObject(apiObject.logo);
            this._headerImageLocator = HeaderImageLocator.CreateFromHeaderImageObject(apiObject.header);
            this._homepageURL = apiObject.homepage;
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
