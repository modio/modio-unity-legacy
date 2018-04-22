using System.Collections.Generic;
using SerializeField = UnityEngine.SerializeField;

namespace ModIO
{
    // - Enums -
    public enum ModStatus
    {
        NotAccepted = 0,
        Accepted = 1,
        Archived = 2,
        Deleted = 3,
    }
    public enum ModVisibility
    {
        Hidden = 0,
        Public = 1,
    }
    public enum ModLogoVersion
    {
        FullSize = 0,
        Thumbnail_320x180,
        Thumbnail_640x360,
        Thumbnail_1280x720,
    }
    public enum ModGalleryImageVersion
    {
        FullSize = 0,
        Thumbnail_320x180,
    }

    public enum TeamMemberPermissionLevel
    {
        Guest = 0,
        Member = 1,
        Contributor = 2,
        Manager = 4,
        Leader = 8,
    }

    [System.Serializable]
    public class ModProfile
    {
        // ---------[ INNER CLASSES ]---------
        [System.Serializable]
        public struct RatingSummary
        {
            // ---------[ SERIALIZED MEMBERS ]---------
            [SerializeField] internal int _totalRatingCount;
            [SerializeField] internal int _positiveRatingCount;
            [SerializeField] internal float _weightedAggregate;
            [SerializeField] internal string _displayText;

            // ---------[ FIELDS ]---------
            public int totalRatingCount     { get { return this._totalRatingCount; } }
            public int positiveRatingCount  { get { return this._positiveRatingCount; } }
            public float weightedAggregate  { get { return this._weightedAggregate; } }
            public string displayText       { get { return this._displayText; } }

            // ---------[ ACCESSORS ]---------
            public int GetNegativeRatingCount()
            {
                return this._totalRatingCount - this._positiveRatingCount;
            }
            public float GetPositiveRatingPercentage()
            {
                return (float)this._positiveRatingCount / (float)this._totalRatingCount;
            }
        }

        [System.Serializable]
        public class LogoImageLocator : MultiVersionImageLocator<ModLogoVersion>
        {
            // ---------[ ABSTRACTS ]---------
            protected override int FullSizeVersion() { return (int)ModLogoVersion.FullSize; }

            // ---------[ API OBJECT INTERFACE ]---------
            public void ApplyLogoObjectValues(API.LogoObject apiObject)
            {
                this._fileName = apiObject.filename;
                this._versionPairing = new VersionSourcePair[]
                {
                    new VersionSourcePair()
                    {
                        versionId = (int)ModLogoVersion.FullSize,
                        source = apiObject.original
                    },
                    new VersionSourcePair()
                    {
                        versionId = (int)ModLogoVersion.Thumbnail_320x180,
                        source = apiObject.thumb_320x180
                    },
                    new VersionSourcePair()
                    {
                        versionId = (int)ModLogoVersion.Thumbnail_640x360,
                        source = apiObject.thumb_640x360
                    },
                    new VersionSourcePair()
                    {
                        versionId = (int)ModLogoVersion.Thumbnail_1280x720,
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
        public class GalleryImageLocator : MultiVersionImageLocator<ModGalleryImageVersion>
        {
            // ---------[ ABSTRACTS ]---------
            protected override int FullSizeVersion() { return (int)ModGalleryImageVersion.FullSize; }

            // ---------[ API OBJECT INTERFACE ]---------
            public void ApplyImageObjectValues(API.ImageObject apiObject)
            {
                this._fileName = apiObject.filename;
                this._versionPairing = new VersionSourcePair[]
                {
                    new VersionSourcePair()
                    {
                        versionId = (int)ModGalleryImageVersion.FullSize,
                        source = apiObject.original
                    },
                    new VersionSourcePair()
                    {
                        versionId = (int)ModGalleryImageVersion.Thumbnail_320x180,
                        source = apiObject.thumb_320x180
                    },
                };
            }
            public static GalleryImageLocator CreateFromImageObject(API.ImageObject apiObject)
            {
                var retVal = new GalleryImageLocator();
                retVal.ApplyImageObjectValues(apiObject);
                return retVal;
            }
        }

        [System.Serializable]
        public class MetadataKVP
        {
            public string key;
            public string value;
        }

        [System.Serializable]
        public class TeamMember
        {
            // ---------[ SERIALIZED MEMBERS ]---------
            [SerializeField] internal int _userId;
            [SerializeField] internal TeamMemberPermissionLevel _permissionLevel;
            [SerializeField] internal TimeStamp _dateAdded;
            [SerializeField] internal string _title;

            // ---------[ FIELDS ]---------
            public int userId                                   { get { return this._userId; } }
            public TeamMemberPermissionLevel permissionLevel    { get { return this._permissionLevel; } }
            public TimeStamp dateAdded                          { get { return this._dateAdded; } }
            public string title                                 { get { return this._title; } }
        }

        // ---------[ SERIALIZED MEMBERS ]---------
        [SerializeField] private int _id;
        [SerializeField] private int _gameId;
        [SerializeField] private ModStatus _status;
        [SerializeField] private ModVisibility _visibility;
        [SerializeField] private int _submittedById;
        [SerializeField] private TimeStamp _dateAdded;
        [SerializeField] private TimeStamp _dateUpdated;
        [SerializeField] private TimeStamp _dateLive;
        [SerializeField] private string _homepageURL;
        [SerializeField] private string _name;
        [SerializeField] private string _nameId;
        [SerializeField] private string _summary;
        [SerializeField] private string _description;
        [SerializeField] private string _metadataBlob;
        [SerializeField] private string _profileURL;
        [SerializeField] private int _primaryModfileId;
        [SerializeField] private RatingSummary _ratingSummary;
        [SerializeField] private string[] _tags;
        [SerializeField] private TeamMember[] _teamMembers;
        [SerializeField] private LogoImageLocator _logoLocator;
        [SerializeField] private string[] _youtubeURLs;
        [SerializeField] private string[] _sketchfabURLs;
        [SerializeField] private GalleryImageLocator[] _galleryImageLocators;
        [SerializeField] private MetadataKVP[] _metadataKVPs;

        // ---------[ FIELDS ]---------
        public int id                               { get { return this._id; } }
        public int gameId                           { get { return this._gameId; } }
        public ModStatus status                     { get { return this._status; } }
        public ModVisibility visibility             { get { return this._visibility; } }
        public int submittedById                    { get { return this._submittedById; } }
        public TimeStamp dateAdded                  { get { return this._dateAdded; } }
        public TimeStamp dateUpdated                { get { return this._dateUpdated; } }
        public TimeStamp dateLive                   { get { return this._dateLive; } }
        public string homepageURL                   { get { return this._homepageURL; } }
        public string name                          { get { return this._name; } }
        public string nameId                        { get { return this._nameId; } }
        public string summary                       { get { return this._summary; } }
        public string description                   { get { return this._description; } }
        public string metadataBlob                  { get { return this._metadataBlob; } }
        public string profileURL                    { get { return this._profileURL; } }
        public int primaryModfileId                 { get { return this._primaryModfileId; } }
        public RatingSummary ratingSummary          { get { return this._ratingSummary; } }
        public ICollection<string> tags             { get { return new List<string>(this._tags); } }
        public ICollection<TeamMember> teamMembers  { get { return new List<TeamMember>(this._teamMembers); } }
        // - Media -
        public LogoImageLocator logoLocator         { get { return this._logoLocator; } }
        public ICollection<string> youtubeURLs      { get { return new List<string>(this._youtubeURLs); } }
        public ICollection<string> sketchfabURLs    { get { return new List<string>(this._sketchfabURLs); } }
        public ICollection<GalleryImageLocator> galleryImageLocators
                                                    { get { return new List<GalleryImageLocator>(this._galleryImageLocators); } }
        
        // - Accessors -
        public Dictionary<string, string> GenerateMetadataKVPDictionary()
        {
            var retVal = new Dictionary<string, string>();
            foreach(MetadataKVP kvp in this._metadataKVPs)
            {
                retVal.Add(kvp.key, kvp.value);
            }
            return retVal;
        }
        public GalleryImageLocator GetGalleryImageWithFileName(string fileName)
        {
            foreach(var locator in this._galleryImageLocators)
            {
                if(locator.fileName == fileName)
                {
                    return locator;
                }
            }
            return null;
        }

        // ---------[ API OBJECT INTERFACE ]---------
        public void ApplyModObjectValues(API.ModObject apiObject)
        {
            this._id = apiObject.id;
            this._gameId = apiObject.game_id;
            this._status = (ModStatus)apiObject.status;
            this._visibility = (ModVisibility)apiObject.visible;
            this._submittedById = apiObject.submitted_by.id;
            this._dateAdded = TimeStamp.GenerateFromServerTimeStamp(apiObject.date_added);
            this._dateUpdated = TimeStamp.GenerateFromServerTimeStamp(apiObject.date_updated);
            this._dateLive = TimeStamp.GenerateFromServerTimeStamp(apiObject.date_live);
            this._homepageURL = apiObject.homepage;
            this._name = apiObject.name;
            this._nameId = apiObject.name_id;
            this._summary = apiObject.summary;
            this._description = apiObject.description;
            this._metadataBlob = apiObject.metadata_blob;
            this._profileURL = apiObject.profile_url;
            this._primaryModfileId = apiObject.modfile.id;

            this._ratingSummary._totalRatingCount = apiObject.rating_summary.total_ratings;
            this._ratingSummary._positiveRatingCount = apiObject.rating_summary.positive_ratings;
            this._ratingSummary._weightedAggregate = apiObject.rating_summary.weighted_aggregate;
            this._ratingSummary._displayText = apiObject.rating_summary.display_text;

            // - Tags -
            if(apiObject.tags != null)
            {
                this._tags = new string[apiObject.tags.Length];
                for(int i = 0; i < apiObject.tags.Length; ++i)
                {
                    this._tags[i] = apiObject.tags[i].name;
                }
            }
            else
            {
                this._tags = new string[0];
            }

            // - Media -
            this._logoLocator = LogoImageLocator.CreateFromLogoObject(apiObject.logo);
            this._youtubeURLs = Utility.SafeCopyArrayOrZero(apiObject.media.youtube);
            this._sketchfabURLs = Utility.SafeCopyArrayOrZero(apiObject.media.sketchfab);

            if(apiObject.media.images != null)
            {
                this._galleryImageLocators = new GalleryImageLocator[apiObject.media.images.Length];
                for(int i = 0; i < apiObject.media.images.Length; ++i)
                {
                    var imageObject = apiObject.media.images[i];
                    this._galleryImageLocators[i] = GalleryImageLocator.CreateFromImageObject(imageObject);
                }
            }
            else
            {
                this._galleryImageLocators = new GalleryImageLocator[0];
            }
        }

        public void ApplyMetadataKVPObjectValues(API.MetadataKVPObject[] apiObjectArray)
        {
            this._metadataKVPs = new MetadataKVP[apiObjectArray.Length];
            for(int i = 0; i < apiObjectArray.Length; ++i)
            {
                this._metadataKVPs[i] = new MetadataKVP()
                {
                    key = apiObjectArray[i].metakey,
                    value = apiObjectArray[i].metavalue,
                };
            }
        }

        public void ApplyTeamMemberObjectValues(API.TeamMemberObject[] apiObjectArray)
        {
            Utility.SafeMapArraysOrZero(apiObjectArray,
                                        (o) =>
                                        {
                                            var tm = new TeamMember()
                                            {
                                                _userId = o.user.id,
                                                _permissionLevel = (TeamMemberPermissionLevel)o.level,
                                                _dateAdded = TimeStamp.GenerateFromServerTimeStamp(o.date_added),
                                                _title = o.position,
                                            };
                                            return tm;
                                        },
                                        out this._teamMembers);
        }

        public static ModProfile CreateFromModObject(API.ModObject apiObject)
        {
            ModProfile profile = new ModProfile();
            profile.ApplyModObjectValues(apiObject);
            return profile;
        }
    }
}