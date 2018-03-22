using System;
using System.Collections.Generic;
using ModIO.API;

namespace ModIO
{
    [Serializable]
    public class ModInfo : IEquatable<ModInfo>, IAPIObjectWrapper<ModObject>, UnityEngine.ISerializationCallbackReceiver
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
        protected ModObject _data = new ModObject();

        public int id                       { get { return _data.id; } }
        public int gameId                   { get { return _data.game_id; } }
        public Status status                { get { return (Status)_data.status; } }
        public Visibility visibility        { get { return (Visibility)_data.visible; } }
        public User submittedBy             { get; protected set; }
        public TimeStamp dateAdded          { get; protected set; }
        public TimeStamp dateUpdated        { get; protected set; }
        public TimeStamp dateLive           { get; protected set; }
        public LogoURLInfo logo             { get; protected set; }
        public string homepage              { get { return _data.homepage; } }
        public string name                  { get { return _data.name; } }
        public string nameId                { get { return _data.name_id; } }
        public string summary               { get { return _data.summary; } }
        public string description           { get { return _data.description; } }
        public string metadataBlob          { get { return _data.metadata_blob; } }
        public string profileURL            { get { return _data.profile_url; } }
        public Modfile modfile              { get; protected set; }
        public ModMediaURLInfo media        { get; protected set; }
        public RatingSummary ratingSummary  { get; protected set; }
        public ModTag[] tags                { get; protected set; }

        public string logoIdentifier = string.Empty;

        // - Accessors -
        public string[] GetTagNames()
        {
            int tagCount = (tags == null ? 0 : tags.Length);
            
            string[] tagNames = new string[tagCount];

            for(int i = 0;
                i < tagCount;
                ++i)
            {
                tagNames[i] = tags[i].name;
            }

            return tagNames;
        }

        // - Initializer -
        protected void InitializeFields()
        {
            this.submittedBy = new User();
            this.submittedBy.WrapAPIObject(_data.submitted_by);
            this.dateAdded = TimeStamp.GenerateFromServerTimeStamp(_data.date_added);
            this.dateUpdated = TimeStamp.GenerateFromServerTimeStamp(_data.date_updated);
            this.dateLive = TimeStamp.GenerateFromServerTimeStamp(_data.date_live);
            this.logo = new LogoURLInfo();
            this.logo.WrapAPIObject(_data.logo);
            this.modfile = Modfile.CreateFromAPIObject(_data.modfile);
            this.media = new ModMediaURLInfo();
            this.media.WrapAPIObject(_data.media);
            this.ratingSummary = new RatingSummary();
            this.ratingSummary.WrapAPIObject(_data.rating_summary);
            
            int tagCount = (_data.tags == null ? 0 : _data.tags.Length);
            this.tags = new ModTag[tagCount];
            for(int i = 0;
                i < tagCount;
                ++i)
            {
                this.tags[i] = new ModTag();
                this.tags[i].WrapAPIObject(_data.tags[i]);
            }

            this.logoIdentifier = ModImageIdentifier.GenerateForModLogo(_data.id);
        }

        // - IAPIObjectWrapper Interface -
        public virtual void WrapAPIObject(ModObject apiObject)
        {
            this._data = apiObject;
            this.InitializeFields();
        }


        public ModObject GetAPIObject()
        {
            return this._data;
        }

        // - ISerializationCallbackReceiver -
        public void OnBeforeSerialize() {}
        public void OnAfterDeserialize()
        {
            this.InitializeFields();
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

        public static EditableModFields CreateEMF(ModInfo info)
        {
            var retVal = new EditableModFields();
            retVal.status.value = (ModStatus)(int)info.status;
            retVal.visibility.value = (ModVisibility)(int)info.visibility;
            retVal.name.value = info.name;
            retVal.nameId.value = info.nameId;
            retVal.summary.value = info.summary;
            retVal.description.value = info.description;
            retVal.homepage.value = info.homepage;
            retVal.metadataBlob.value = info.metadataBlob;
            retVal.tags.value = info.GetTagNames();
            retVal.logoIdentifier.value = info.logoIdentifier;
            retVal.youtubeURLs.value = info.media.youtubeURLs;
            retVal.sketchfabURLs.value = info.media.sketchfabURLs;
            // retVal.imageIdentifiers.value = info.media.imageIdentifiers;
            return retVal;
        }
    }
}
