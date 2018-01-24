using System;

namespace ModIO
{
    [Serializable]
    public class ModInfo : IEquatable<ModInfo>, IAPIObjectWrapper<API.ModObject>
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


        // - IAPIObjectWrapper Interface -
        public void WrapAPIObject(API.ModObject apiObject)
        {
            this._data = apiObject;

            this.submittedBy = new User();
            this.submittedBy.WrapAPIObject(apiObject.submitted_by);
            this.dateAdded = TimeStamp.GenerateFromServerTimeStamp(apiObject.date_added);
            this.dateUpdated = TimeStamp.GenerateFromServerTimeStamp(apiObject.date_updated);
            this.dateLive = TimeStamp.GenerateFromServerTimeStamp(apiObject.date_live);
            this.logo = new LogoURLInfo();
            this.logo.WrapAPIObject(apiObject.logo);
            this.modfile = new Modfile();
            this.modfile.WrapAPIObject(apiObject.modfile);
            this.media = new ModMediaInfo();
            this.media.WrapAPIObject(apiObject.media);
            this.ratingSummary = new RatingSummary();
            this.ratingSummary.WrapAPIObject(apiObject.rating_summary);
            
            this.tags = new ModTag[apiObject.tags.Length];
            this.tagNames = new string[apiObject.tags.Length];
            for(int i = 0;
                i < apiObject.tags.Length;
                ++i)
            {
                this.tags[i]      = new ModTag();
                this.tags[i].WrapAPIObject(apiObject.tags[i]);
                this.tagNames[i]  = apiObject.tags[i].name;
            }
        }

        public API.ModObject GetAPIObject()
        {
            return this._data;
        }
        
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
