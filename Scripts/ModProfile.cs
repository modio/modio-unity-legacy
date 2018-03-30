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
        public struct MetadataKVP
        {
            public string key;
            public string value;
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
        [SerializeField] private string _logoIdentifier;
        [SerializeField] private string[] _youtubeURLs;
        [SerializeField] private string[] _sketchfabURLs;
        [SerializeField] private string[] _imageIdentifiers;
        [SerializeField] private int[] _dependencies;
        [SerializeField] private MetadataKVP[] _metadataKVPs;
        // TODO(@jackson): TeamMembers

        // ---------[ FIELDS ]---------
        public int id                                   { get { return this._id; } }
        public int gameId                               { get { return this._gameId; } }
        public ModStatus status                         { get { return this._status; } }
        public ModVisibility visibility                 { get { return this._visibility; } }
        public int submittedById                        { get { return this._submittedById; } }
        public TimeStamp dateAdded                      { get { return this._dateAdded; } }
        public TimeStamp dateUpdated                    { get { return this._dateUpdated; } }
        public TimeStamp dateLive                       { get { return this._dateLive; } }
        public string homepageURL                       { get { return this._homepageURL; } }
        public string name                              { get { return this._name; } }
        public string nameId                            { get { return this._nameId; } }
        public string summary                           { get { return this._summary; } }
        public string description                       { get { return this._description; } }
        public string metadataBlob                      { get { return this._metadataBlob; } }
        public string profileURL                        { get { return this._profileURL; } }
        public int primaryModfileId                     { get { return this._primaryModfileId; } }
        public RatingSummary ratingSummary              { get { return this._ratingSummary; } }
        public ICollection<string> tags                 { get { return new List<string>(this._tags); } }
        public string logoIdentifier                    { get { return this._logoIdentifier; } }
        public ICollection<string> youtubeURLs          { get { return new List<string>(this._youtubeURLs); } }
        public ICollection<string> sketchfabURLs        { get { return new List<string>(this._sketchfabURLs); } }
        public ICollection<string> imageIdentifiers     { get { return new List<string>(this._imageIdentifiers); } }
        public ICollection<int> dependencies            { get { return new List<int>(this._dependencies); } }

        public Dictionary<string, string> GetMetadataKVPs()
        {
            var retVal = new Dictionary<string, string>();
            foreach(MetadataKVP kvp in this._metadataKVPs)
            {
                retVal.Add(kvp.key, kvp.value);
            }
            return retVal;
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
            this._tags = new string[apiObject.tags.Length];
            for(int i = 0; i < apiObject.tags.Length; ++i)
            {
                this._tags[i] = apiObject.tags[i].name;
            }

            // - Media -
            this._logoIdentifier = ModImageIdentifier.GenerateForModLogo(apiObject.id);
            this._youtubeURLs = apiObject.media.youtube;
            this._sketchfabURLs = apiObject.media.sketchfab;

            if(apiObject.media.images != null)
            {
                this._imageIdentifiers = new string[apiObject.media.images.Length];
                for(int i = 0; i < apiObject.media.images.Length; ++i)
                {
                    this._imageIdentifiers[i] = ModImageIdentifier.GenerateForModMedia(apiObject.id,
                                                                                       apiObject.media.images[i].filename);
                }
            }
            else
            {
                this._imageIdentifiers = new string[0];
            }
        }

        public void ApplyModDependencyObjectValues(API.ModDependencyObject[] apiObjectArray)
        {
            this._dependencies = new int[apiObjectArray.Length];
            for(int i = 0; i < apiObjectArray.Length; ++i)
            {
                this._dependencies[i] = apiObjectArray[i].mod_id;
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

        public static ModProfile CreateFromModObject(API.ModObject apiObject)
        {
            ModProfile profile = new ModProfile();
            profile.ApplyModObjectValues(apiObject);
            return profile;
        }
    }
}