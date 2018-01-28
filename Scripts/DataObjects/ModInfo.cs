using System;
using System.Collections.Generic;

namespace ModIO
{
    [Serializable]
    public class ModInfo : IEquatable<ModInfo>, IAPIObjectWrapper<API.ModObject>, UnityEngine.ISerializationCallbackReceiver
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
        protected API.ModObject _data;
        [UnityEngine.SerializeField]
        protected string[] tagNames;

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
        public ModMediaInfo media           { get; protected set; }
        public RatingSummary ratingSummary  { get; protected set; }
        public ModTag[] tags                { get; protected set; }


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
            
            int tagCount = (apiObject.tags == null ? 0 : apiObject.tags.Length);
            this.tags = new ModTag[tagCount];
            this.tagNames = new string[tagCount];
            for(int i = 0;
                i < tagCount;
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

        public string[] GetTagNames()
        {
            return this.tagNames;
        }

        // - ISerializationCallbackReceiver -
        public void OnBeforeSerialize() {}
        public void OnAfterDeserialize()
        {
            this.WrapAPIObject(this._data);
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

    public class EditableModInfo : ModInfo
    {
        public static EditableModInfo FromModInfo(ModInfo modInfo)
        {
            EditableModInfo newEMI = new EditableModInfo();

            newEMI.WrapAPIObject(modInfo.GetAPIObject());

            newEMI.modfileId = modInfo.modfile.id;

            newEMI.modfile = null;

            return newEMI;
        }

        // - Put Request Values -
        private Dictionary<string, string> putValues = new Dictionary<string, string>();
        public Dictionary<string, string> AsPutRequestValues()
        {
            return putValues;
        }

        // --- Extra Fields ---
        private int modfileId = 0;

        // --- GETTERS ---
        public int GetModfileId()
        {
            return modfileId;
        }

        // --- SETTERS ---
        // Status of a mod. The mod must have at least one uploaded modfile to be 'accepted' or 'archived' (best if this field is controlled by game admins, see status and visibility for details):
        public void SetStatus(Status value)
        {
            UnityEngine.Debug.Assert(value != Status.Deleted,
                                     "Status.Deleted cannot be set via SetStatus. Use the APIClient.DeleteMod instead");

            _data.status = (int)value;

            putValues["status"] = ((int)value).ToString();
        }
        // Visibility of the mod (best if this field is controlled by mod admins, see status and visibility for details):
        public void SetVisibility(Visibility value)
        {
            _data.visible = (int)value;

            putValues["visible"] = ((int)value).ToString();
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

            putValues["name"] = value;
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

            putValues["name_id"] = value;
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

            putValues["summary"] = value;
        }
        // Detailed description for your mod, which can include details such as 'About', 'Features', 'Install Instructions', 'FAQ', etc. HTML supported and encouraged.
        public void SetDescription(string value)
        {
            _data.description = value;

            putValues["description"] = value;
        }
        // Official homepage for your mod. Must be a valid URL.
        public void SetHomepage(string value)
        {
            if(!Utility.IsURL(value))
            {
                UnityEngine.Debug.LogWarning(value + " is not a valid URL and will not be accepted by the API.");
                value = "";
            }

            _data.homepage = value;

            putValues["homepage"] = value;
        }
        // Unique id of the Modfile Object to be labelled as the current release.
        public void SetModfileID(int value)
        {
            modfileId = value;

            putValues["stock"] = ((int)value).ToString();
        }
        // Artificially limit the amount of times the mod can be subscribed too.
        public void SetStock(int value)
        {
            putValues["modfile"] = ((int)value).ToString();
        }
    }

    [Serializable]
    public class AddableModInfo
    {
        // - Fields -
        [UnityEngine.SerializeField]
        private API.CreatedModObject _data;

        // Visibility of the mod (best if this field is controlled by mod admins, see status and visibility for details):
        public ModInfo.Visibility visibility
        {
            get { return (ModInfo.Visibility)_data.visible; }
            set { _data.visible = (int)value; }
        }
        // Name of your mod.
        public string name
        {
            get { return _data.name; }
            set { _data.name = value; }
        }
        // Path for the mod on mod.io. For example: https://gamename.mod.io/mod-name-id-here. If no name_id is specified the name will be used. For example: 'Stellaris Shader Mod' will become 'stellaris-shader-mod'. Cannot exceed 80 characters.
        public string nameId
        {
            get { return _data.name_id; }
            set { _data.name_id = value; }
        }
        // Summary for your mod, giving a brief overview of what it's about. Cannot exceed 250 characters.
        public string summary
        {
            get { return _data.summary; }
            set { _data.summary = value; }
        }
        // Detailed description for your mod, which can include details such as 'About', 'Features', 'Install Instructions', 'FAQ', etc. HTML supported and encouraged.
        public string description
        {
            get { return _data.description; }
            set { _data.description = value; }
        }
        // Official homepage for your mod. Must be a valid URL.
        public string homepage
        {
            get { return _data.homepage; }
            set { _data.homepage = value; }
        }
        // Artificially limit the amount of times the mod can be subscribed too.
        public int stock
        {
            get { return _data.stock; }
            set { _data.stock = value; }
        }
        // Metadata stored by the game developer which may include properties as to how the item works, or other information you need to display. Metadata can also be stored as searchable key value pairs, and to individual mod files.
        public string metadata
        {
            get { return _data.metadata; }
            set { _data.metadata = value; }
        }
        // An array of strings that represent what the mod has been tagged as. Only tags that are supported by the parent game can be applied. To determine what tags are eligible, see the tags values within tag_options column on the parent Game Object.
        public List<string> tagNames = new List<string>();
        // Image file which will represent your mods logo. Must be gif, jpg or png format and cannot exceed 8MB in filesize. Dimensions must be at least 640x360 and we recommended you supply a high resolution image with a 16 / 9 ratio. mod.io will use this image to make three thumbnails for the dimensions 320x180, 640x360 and 1280x720.
        public string logoFilepath = "";

        // --- ACCESSORS ---
        public Dictionary<string, string> GetValueFields()
        {
            Dictionary<string, string> retVal = new Dictionary<string, string>();
            retVal["visible"] = _data.visible.ToString();
            retVal["name"] = _data.name;
            retVal["name_id"] = _data.name_id;
            retVal["summary"] = _data.summary;
            retVal["description"] = _data.description;
            retVal["homepage"] = _data.homepage;
            retVal["stock"] = _data.stock.ToString();
            retVal["metadata"] = _data.metadata;

            if(tagNames.Count > 0)
            {
                retVal["tags"] = tagNames[0];
                for(int i = 1; i < tagNames.Count; ++i)
                {
                    retVal["tags"] += "," + tagNames[i];
                }
            }
            return retVal;
        }
        public Dictionary<string, BinaryData> GetDataFields()
        {
            Dictionary<string, BinaryData> retVal = new Dictionary<string, BinaryData>();
            retVal["logo"] = GetLogoBinaryData();
            return retVal;
        }

        public BinaryData GetLogoBinaryData()
        {
            if(System.IO.File.Exists(logoFilepath))
            {
                BinaryData newData = new BinaryData();
                newData.contents = System.IO.File.ReadAllBytes(logoFilepath);
                newData.fileName = System.IO.Path.GetFileName(logoFilepath);
                return newData;
            }
            return null;
        }
    }
}
