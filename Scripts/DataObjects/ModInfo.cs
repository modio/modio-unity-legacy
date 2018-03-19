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
    }

    [Serializable]
    public class EditableModInfo : ModInfo
    {
        public static EditableModInfo FromModInfo(ModInfo modInfo)
        {
            return EditableModInfo.FromModObject(modInfo.GetAPIObject());
        }

        public static EditableModInfo FromModObject(API.ModObject modObject)
        {
            EditableModInfo newEMI = new EditableModInfo();

            newEMI.WrapAPIObject(modObject);

            return newEMI;
        }

        public override void WrapAPIObject(ModObject apiObject)
        {
            base.WrapAPIObject(apiObject);
            this._initialData = apiObject.Clone();
            this.logoFilepath = "";
        }

        // --- Additional Fields ---
        [UnityEngine.SerializeField] private ModObject _initialData;
        [UnityEngine.SerializeField] private string logoFilepath;

        public string unsubmittedLogoFilepath { get { return logoFilepath; } }

        // TODO(@jackson): Add Support for Mod Media

        // --- SETTERS ---
        // Status of a mod. The mod must have at least one uploaded modfile to be 'accepted' or 'archived' (best if this field is controlled by game admins, see status and visibility for details):
        public void SetStatus(Status value)
        {
            UnityEngine.Debug.Assert(value != Status.Deleted,
                                     "Status.Deleted cannot be set via SetStatus. Use the APIClient.DeleteMod instead");

            _data.status = (int)value;
        }
        // Visibility of the mod (best if this field is controlled by mod admins, see status and visibility for details):
        public void SetVisibility(Visibility value)
        {
            _data.visible = (int)value;
        }
        // Name of your mod. Cannot exceed 80 characters.
        public void SetName(string value)
        {
            if(value.Length > 80)
            {
                value = value.Substring(0, 80);
                UnityEngine.Debug.LogWarning("ModInfo.name cannot exceed 80 characters. Truncating.");
            }

            _data.name = value;
        }
        // Path for the mod on mod.io. For example: https://gamename.mod.io/mod-name-id-here. Cannot exceed 80 characters.
        public void SetNameID(string value)
        {
            if(value.Length > 80)
            {
                value = value.Substring(0, 80);
                UnityEngine.Debug.LogWarning("ModInfo.nameId cannot exceed 80 characters. Truncating.");
            }

            _data.name_id = value;
        }
        // Summary for your mod, giving a brief overview of what it's about. Cannot exceed 250 characters.
        public void SetSummary(string value)
        {
            if(value.Length > 250)
            {
                value = value.Substring(0, 250);
                UnityEngine.Debug.LogWarning("ModInfo.summary cannot exceed 250 characters. Truncating.");
            }

            _data.summary = value;
        }
        // Detailed description for your mod, which can include details such as 'About', 'Features', 'Install Instructions', 'FAQ', etc. HTML supported and encouraged.
        public void SetDescription(string value)
        {
            _data.description = value;
        }
        // Official homepage for your mod. Must be a valid URL.
        public void SetHomepage(string value)
        {
            if(!Utility.IsURL(value))
            {
                UnityEngine.Debug.LogWarning(value + " is not a valid URL and will not be accepted by the ");
                value = "";
            }

            _data.homepage = value;
        }
        // // Unique id of the Modfile Object to be labelled as the current release.
        // public void SetModfile(Modfile value)
        // {
        //     _data.modfile = value.GetAPIObject();
        // }
        // Artificially limit the amount of times the mod can be subscribed too.
        public void SetStock(int value)
        {
            _data.stock = value;
        }
        // Metadata stored by the game developer which may include properties as to how the item works, or other information you need to display. Metadata can also be stored as searchable key value pairs, and to individual mod files.
        public void SetMetadataBlob(string value)
        {
            _data.metadata_blob = value;
        }
        // An array of strings that represent what the mod has been tagged as. Only tags that are supported by the parent game can be applied. To determine what tags are eligible, see the tags values within tag_options column on the parent Game Object.
        public void SetTagNames(string[] valueArray)
        {
            ModTagObject[] modTagArray = new ModTagObject[valueArray.Length];

            for(int i = 0; i < valueArray.Length; ++i)
            {
                ModTagObject tag = new ModTagObject();
                tag.name = valueArray[i];
                tag.date_added = TimeStamp.Now().AsServerTimeStamp();
                modTagArray[i] = tag;
            }

            _data.tags = modTagArray;
        }

        public void SetLogoFilepath(string value)
        {
            logoFilepath = value;
        }

        // TODO(@jackson): Complete
        public void SetModMedia()
        {

        }

        // --- SUBMISSION HELPERS ---
        public bool isInfoDirty()
        {
            return (_data.status != _initialData.status
                    || _data.visible != _initialData.visible
                    || _data.name != _initialData.name
                    || _data.name_id != _initialData.name_id
                    || _data.summary != _initialData.summary
                    || _data.description != _initialData.description
                    || _data.homepage != _initialData.homepage
                    || _data.stock != _initialData.stock
                    || _data.metadata_blob != _initialData.metadata_blob);
        }

        public bool isMediaDirty()
        {
            return (!_data.media.Equals(_initialData.media));
        }

        public bool isTagsDirty()
        {
            bool isDirty = (_data.tags.Length != _initialData.tags.Length);
            for(int i = 0;
                !isDirty && i < _data.tags.Length;
                ++i)
            {
                isDirty |= (_data.tags[i].name != _initialData.tags[i].name);
            }

            return isDirty;
        }

        public StringValueParameter[] GetEditValueFields()
        {
            List<StringValueParameter> retVal = new List<StringValueParameter>();

            if(_initialData.status != _data.status)
            {
                retVal.Add(StringValueParameter.Create("status", _data.status.ToString()));
            }
            if(_initialData.visible != _data.visible)
            {
                retVal.Add(StringValueParameter.Create("visible", _data.visible.ToString()));
            }
            if(_initialData.name != _data.name)
            {
                retVal.Add(StringValueParameter.Create("name", _data.name));
            }
            if(_initialData.name_id != _data.name_id)
            {
                retVal.Add(StringValueParameter.Create("name_id", _data.name_id));
            }
            if(_initialData.summary != _data.summary)
            {
                retVal.Add(StringValueParameter.Create("summary", _data.summary));
            }
            if(_initialData.description != _data.description)
            {
                retVal.Add(StringValueParameter.Create("description", _data.description));
            }
            if(_initialData.homepage != _data.homepage)
            {
                retVal.Add(StringValueParameter.Create("homepage", _data.homepage));
            }
            if(_initialData.stock != _data.stock)
            {
                retVal.Add(StringValueParameter.Create("stock", _data.stock.ToString()));
            }
            if(_initialData.metadata_blob != _data.metadata_blob)
            {
                retVal.Add(StringValueParameter.Create("metadata_blob", _data.metadata_blob));
            }

            return retVal.ToArray();
        }

        public StringValueParameter[] GetAddValueFields()
        {
            List<StringValueParameter> retVal = new List<StringValueParameter>(8 + tags.Length);

            retVal.Add(StringValueParameter.Create("visible", _data.visible));
            retVal.Add(StringValueParameter.Create("name", _data.name));
            retVal.Add(StringValueParameter.Create("name_id", _data.name_id));
            retVal.Add(StringValueParameter.Create("summary", _data.summary));
            retVal.Add(StringValueParameter.Create("description", _data.description));
            retVal.Add(StringValueParameter.Create("homepage", _data.homepage));
            retVal.Add(StringValueParameter.Create("stock", _data.stock));
            retVal.Add(StringValueParameter.Create("metadata_blob", _data.metadata_blob));

            string[] tagNames = this.GetTagNames();
            foreach(string tagName in tagNames)
            {
                retVal.Add(StringValueParameter.Create("tags[]", tagName));
            }

            return retVal.ToArray();
        }

        public BinaryDataParameter[] GetAddDataFields()
        {
            List<BinaryDataParameter> retVal = new List<BinaryDataParameter>(1);

            if(System.IO.File.Exists(logoFilepath))
            {
                BinaryDataParameter newData = new BinaryDataParameter();
                newData.key = "logo";
                newData.contents = System.IO.File.ReadAllBytes(logoFilepath);
                newData.fileName = System.IO.Path.GetFileName(logoFilepath);
                
                retVal.Add(newData);
            }

            return retVal.ToArray();
        }

        public string[] GetAddedTags()
        {
            List<string> addedTags = new List<string>(this.GetTagNames());
            foreach(ModTagObject tag in _initialData.tags)
            {
                addedTags.Remove(tag.name);
            }
            return addedTags.ToArray();
        }

        public ModMediaChanges GetAddedMedia()
        {
            ModMediaChanges changes = new ModMediaChanges();
            changes.modId = id;

            // - YouTube -
            List<string> addedYouTubeLinks = new List<string>(_data.media.youtube);
            foreach(string youtubeLink in _initialData.media.youtube)
            {
                addedYouTubeLinks.Remove(youtubeLink);
            }
            changes.youtube = addedYouTubeLinks.ToArray();

            // - Sketchfab -
            List<string> addedSketchfabLinks = new List<string>(_data.media.sketchfab);
            foreach(string sketchfabLink in _initialData.media.sketchfab)
            {
                addedSketchfabLinks.Remove(sketchfabLink);
            }
            changes.sketchfab = addedSketchfabLinks.ToArray();
            
            // - Image -
            List<string> addedImages = new List<string>();
            foreach(ImageObject imageObject in _data.media.images)
            {
                addedImages.Add(imageObject.original);
            }
            foreach(ImageObject imageObject in _initialData.media.images)
            {
                addedImages.Remove(imageObject.original);
            }
            changes.images = addedImages.ToArray();

            return changes;
        }

        public ModMediaChanges GetRemovedMedia()
        {
            ModMediaChanges changes = new ModMediaChanges();
            changes.modId = id;

            // - YouTube -
            List<string> removedYouTubeLinks = new List<string>(_initialData.media.youtube);
            foreach(string youtubeLink in _data.media.youtube)
            {
                removedYouTubeLinks.Remove(youtubeLink);
            }
            changes.youtube = removedYouTubeLinks.ToArray();

            // - Sketchfab -
            List<string> removedSketchfabLinks = new List<string>(_initialData.media.sketchfab);
            foreach(string sketchfabLink in _data.media.sketchfab)
            {
                removedSketchfabLinks.Remove(sketchfabLink);
            }
            changes.sketchfab = removedSketchfabLinks.ToArray();
            
            // - Image -
            List<string> removedImages = new List<string>();
            foreach(ImageObject imageObject in _data.media.images)
            {
                removedImages.Add(imageObject.filename);
            }
            foreach(ImageObject imageObject in _data.media.images)
            {
                removedImages.Remove(imageObject.filename);
            }
            changes.images = removedImages.ToArray();

            return changes;
        }

        public AddModParameters AsAddModParameters()
        {
            AddModParameters retVal = new AddModParameters();
            retVal.stringValues = new List<StringValueParameter>(GetAddValueFields());
            retVal.binaryData = new List<BinaryDataParameter>(GetAddDataFields());
            return retVal;
        }

        public EditModParameters AsEditModParameters()
        {
            EditModParameters retVal = new EditModParameters();
            retVal.stringValues = new List<StringValueParameter>(GetEditValueFields());
            return retVal;
        }
    }
}
