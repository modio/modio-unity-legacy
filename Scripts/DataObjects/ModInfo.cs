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
        public string[] tagNames            { get; protected set; }


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

    [Serializable]
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

        public Dictionary<string, string> AsPutRequestValues()
        {
            return putValues;
        }

        // --- Extra Fields ---
        private int modfileId = 0;
        
        // - PUT Request Format -
        private Dictionary<string, string> putValues = new Dictionary<string, string>();

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
}
