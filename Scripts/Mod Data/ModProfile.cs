using System.Collections.Generic;
using SerializeField = UnityEngine.SerializeField;

using Newtonsoft.Json;

using ModObject = ModIO.API.ModObject;
using TeamMemberObject = ModIO.API.TeamMemberObject;
using MetadataKVPObject = ModIO.API.MetadataKVPObject;

namespace ModIO
{
    // - Enums -
    // public enum ModStatus
    // {
    //     NotAccepted = ModObject.StatusValue.NotAccepted,
    //     Accepted = ModObject.StatusValue.Accepted,
    //     Archived = ModObject.StatusValue.Archived,
    //     Deleted = ModObject.StatusValue.Deleted,
    // }
    // public enum ModVisibility
    // {
    //     Hidden = ModObject.VisibleValue.Hidden,
    //     Public = ModObject.VisibleValue.Public,
    // }

    [System.Serializable]
    public class ModProfile
    {
        // ---------[ INNER CLASSES ]---------
        [System.Serializable]
        public struct RatingSummary
        {
            // ---------[ SERIALIZED MEMBERS ]---------
            [JsonProperty] internal int _totalRatingCount;
            [JsonProperty] internal int _positiveRatingCount;
            [JsonProperty] internal float _weightedAggregate;
            [JsonProperty] internal string _displayText;

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
        private class MetadataKVP
        {
            public string key;
            public string value;
        }

        // ---------[ SERIALIZED MEMBERS ]---------
        [JsonProperty] private int _id;
        [JsonProperty] private int _gameId;
        [JsonProperty] private ModStatus _status;
        [JsonProperty] private ModVisibility _visibility;
        [JsonProperty] private int _submittedById;
        [JsonProperty] private int _dateAdded;
        [JsonProperty] private int _dateUpdated;
        [JsonProperty] private int _dateLive;
        [JsonProperty] private string _homepageURL;
        [JsonProperty] private string _name;
        [JsonProperty] private string _nameId;
        [JsonProperty] private string _summary;
        [JsonProperty] private string _description;
        [JsonProperty] private string _metadataBlob;
        [JsonProperty] private string _profileURL;
        [JsonProperty] private int _primaryModfileId;
        [JsonProperty] private RatingSummary _ratingSummary;
        [JsonProperty] private string[] _tags;
        [JsonProperty] private TeamMember[] _teamMembers;
        [JsonProperty] private ModLogoImageLocator _logoLocator;
        [JsonProperty] private string[] _youtubeURLs;
        [JsonProperty] private string[] _sketchfabURLs;
        [JsonProperty] private ModGalleryImageLocator[] _galleryImageLocators;
        [JsonProperty] private MetadataKVP[] _metadataKVPs;

        // ---------[ FIELDS ]---------
        [JsonIgnore] public int id                               { get { return this._id; } }
        [JsonIgnore] public int gameId                           { get { return this._gameId; } }
        [JsonIgnore] public ModStatus status                     { get { return this._status; } }
        [JsonIgnore] public ModVisibility visibility             { get { return this._visibility; } }
        [JsonIgnore] public int submittedById                    { get { return this._submittedById; } }
        [JsonIgnore] public int dateAdded                  { get { return this._dateAdded; } }
        [JsonIgnore] public int dateUpdated                { get { return this._dateUpdated; } }
        [JsonIgnore] public int dateLive                   { get { return this._dateLive; } }
        [JsonIgnore] public string homepageURL                   { get { return this._homepageURL; } }
        [JsonIgnore] public string name                          { get { return this._name; } }
        [JsonIgnore] public string nameId                        { get { return this._nameId; } }
        [JsonIgnore] public string summary                       { get { return this._summary; } }
        [JsonIgnore] public string description                   { get { return this._description; } }
        [JsonIgnore] public string metadataBlob                  { get { return this._metadataBlob; } }
        [JsonIgnore] public string profileURL                    { get { return this._profileURL; } }
        [JsonIgnore] public int primaryModfileId                 { get { return this._primaryModfileId; } }
        [JsonIgnore] public RatingSummary ratingSummary          { get { return this._ratingSummary; } }
        [JsonIgnore] public ICollection<string> tags             { get { return new List<string>(this._tags); } }
        [JsonIgnore] public ICollection<TeamMember> teamMembers  { get { return new List<TeamMember>(this._teamMembers); } }
        // - Media -
        [JsonIgnore] public ModLogoImageLocator logoLocator      { get { return this._logoLocator; } }
        [JsonIgnore] public ICollection<string> youtubeURLs      { get { return new List<string>(this._youtubeURLs); } }
        [JsonIgnore] public ICollection<string> sketchfabURLs    { get { return new List<string>(this._sketchfabURLs); } }
        [JsonIgnore] public ICollection<ModGalleryImageLocator> galleryImageLocators
                                                    { get { return new List<ModGalleryImageLocator>(this._galleryImageLocators); } }
        
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
        public ModGalleryImageLocator GetGalleryImageWithFileName(string fileName)
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
        public void ApplyModObjectValues(ModObject apiObject)
        {
            this._id = apiObject.id;
            this._gameId = apiObject.gameId;
            this._status = apiObject.status;
            this._visibility = apiObject.visibility;
            this._submittedById = apiObject.submittedBy.id;
            this._dateAdded = apiObject.dateAdded;
            this._dateUpdated = apiObject.dateUpdated;
            this._dateLive = apiObject.dateLive;
            this._homepageURL = apiObject.homepageURL;
            this._name = apiObject.name;
            this._nameId = apiObject.nameId;
            this._summary = apiObject.summary;
            this._description = apiObject.description;
            this._metadataBlob = apiObject.metadataBlob;
            this._profileURL = apiObject.profileURL;
            this._primaryModfileId = apiObject.currentRelease.id;

            this._ratingSummary._totalRatingCount = apiObject.ratingSummary.totalRatingCount;
            this._ratingSummary._positiveRatingCount = apiObject.ratingSummary.positiveRatingCount;
            this._ratingSummary._weightedAggregate = apiObject.ratingSummary.weightedAggregate;
            this._ratingSummary._displayText = apiObject.ratingSummary.displayText;

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

            // - Metadata KVPs -
            this._metadataKVPs = new MetadataKVP[apiObject.metadataKVP.Length];
            for(int i = 0; i < apiObject.metadataKVP.Length; ++i)
            {
                this._metadataKVPs[i] = new MetadataKVP()
                {
                    key = apiObject.metadataKVP[i].metakey,
                    value = apiObject.metadataKVP[i].metavalue,
                };
            }

            // - Media -
            this._logoLocator = ModLogoImageLocator.CreateFromLogoObject(apiObject.logo);
            this._youtubeURLs = Utility.SafeCopyArrayOrZero(apiObject.media.youtubeURLs);
            this._sketchfabURLs = Utility.SafeCopyArrayOrZero(apiObject.media.sketchfabURLs);

            if(apiObject.media.galleryImages != null)
            {
                this._galleryImageLocators = new ModGalleryImageLocator[apiObject.media.galleryImages.Length];
                for(int i = 0; i < apiObject.media.galleryImages.Length; ++i)
                {
                    var imageObject = apiObject.media.galleryImages[i];
                    this._galleryImageLocators[i] = ModGalleryImageLocator.CreateFromImageObject(imageObject);
                }
            }
            else
            {
                this._galleryImageLocators = new ModGalleryImageLocator[0];
            }
        }

        public void ApplyTeamMemberObjectValues(TeamMemberObject[] apiObjectArray)
        {
            Utility.SafeMapArraysOrZero(apiObjectArray,
                                        (o) =>
                                        {
                                            var tm = new TeamMember();
                                            tm.ApplyTeamMemberObjectValues(o);
                                            return tm;
                                        },
                                        out this._teamMembers);
        }

        public static ModProfile CreateFromModObject(ModObject apiObject)
        {
            ModProfile profile = new ModProfile();
            profile.ApplyModObjectValues(apiObject);
            return profile;
        }
    }
}