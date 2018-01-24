using System;

namespace ModIO
{
    [Serializable]
    public class ModInfo : IEquatable<ModInfo>
    {
        // - Enums -
        public enum Status
        {
            NotAccepted = 0,
            Accepted = 1,
            Archived = 2,
            Deleted = 3,
        }
        public enum Visibility
        {
            Hidden = 0,
            Public = 1,
        }

        // - Constructors - 
        public static ModInfo GenerateFromAPIObject(API.ModObject apiObject)
        {
            ModInfo newMod = new ModInfo();
            newMod._data = apiObject;

            newMod.submittedBy =    User.GenerateFromAPIObject(apiObject.submitted_by);
            newMod.dateAdded =      TimeStamp.GenerateFromServerTimeStamp(apiObject.date_added);
            newMod.dateUpdated =    TimeStamp.GenerateFromServerTimeStamp(apiObject.date_updated);
            newMod.dateLive =       TimeStamp.GenerateFromServerTimeStamp(apiObject.date_live);
            newMod.logo =           LogoURLInfo.GenerateFromAPIObject(apiObject.logo);
            newMod.modfile =        Modfile.GenerateFromAPIObject(apiObject.modfile);
            newMod.media =          ModMediaInfo.GenerateFromAPIObject(apiObject.media);
            newMod.ratingSummary =  RatingSummary.GenerateFromAPIObject(apiObject.rating_summary);
            
            newMod.tags     = new ModTag[apiObject.tags.Length];
            newMod.tagNames = new string[apiObject.tags.Length];
            for(int i = 0;
                i < apiObject.tags.Length;
                ++i)
            {
                newMod.tags[i]      = ModTag.GenerateFromAPIObject(apiObject.tags[i]);
                newMod.tagNames[i]  = apiObject.tags[i].name;
            }

            return newMod;
        }

        public static ModInfo[] GenerateFromAPIObjectArray(API.ModObject[] apiObjectArray)
        {
            ModInfo[] objectArray = new ModInfo[apiObjectArray.Length];

            for(int i = 0;
                i < apiObjectArray.Length;
                ++i)
            {
                objectArray[i] = ModInfo.GenerateFromAPIObject(apiObjectArray[i]);
            }

            return objectArray;
        }

        // - Fields -
        [UnityEngine.SerializeField]
        private API.ModObject _data;

        public int id                       { get { return _data.id; } }
        public int gameId                   { get { return _data.game_id; } }
        public Status status                { get { return (Status)_data.status; } }
        public Visibility visibility        { get { return (Visibility)_data.visible; } }
        public User submittedBy             { get; private set; }
        public TimeStamp dateAdded          { get; private set; }
        public TimeStamp dateUpdated        { get; private set; }
        public TimeStamp dateLive           { get; private set; }
        public LogoURLInfo logo                { get; private set; }
        public string homepage              { get { return _data.homepage; } }
        public string name                  { get { return _data.name; } }
        public string nameId                { get { return _data.name_id; } }
        public string summary               { get { return _data.summary; } }
        public string description           { get { return _data.description; } }
        public string metadataBlob          { get { return _data.metadata_blob; } }
        public string profileURL            { get { return _data.profile_url; } }
        public Modfile modfile              { get; private set; }
        public ModMediaInfo media           { get; private set; }
        public RatingSummary ratingSummary  { get; private set; }
        public ModTag[] tags                { get; private set; }
        public string[] tagNames            { get; private set; }

        // - Equality Overrides -
        public override int GetHashCode()
        {
            return this._data.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as ModInfo);
        }

        public bool Equals(ModInfo other)
        {
            return (Object.ReferenceEquals(this, other)
                    || this._data.Equals(other._data));
        }
    }
}
