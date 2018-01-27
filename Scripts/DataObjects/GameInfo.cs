using System;
using System.Collections.Generic;

namespace ModIO
{
    [Serializable]
    public class GameInfo : IEquatable<GameInfo>, IAPIObjectWrapper<API.GameObject>, UnityEngine.ISerializationCallbackReceiver
    {
        // - Enums -
        public enum Status
        {
            NotAccepted = 0,
            Accepted = 1,
            Archived = 2,
            Deleted = 3,
        }

        public enum PresentationOption
        {
            GridView = 0,
            TableView = 1,
        }

        public enum SubmissionOption
        {
            ToolOnly = 0,
            Unrestricted = 1,
        }

        public enum CurationOption
        {
            None = 0,
            Paid = 1,
            Full = 2,
        }

        [Flags]
        public enum CommunityOptions
        {
            Disabled = 0,
            DiscussionBoard = 0x01,
            GuidesAndNews = 0x02,
        }

        [Flags]
        public enum RevenueOptions
        {
            Disabled = 0,
            AllowSales = 0x01,
            AllowDonations = 0x02,
            AllowModTrading = 0x04,
            AllowModScarcity = 0x08,
        }

        [Flags]
        public enum APIAccessOptions
        {
            RestrictAll = 0,
            AllowPublicAccess = 1, // This game allows 3rd parties to access the mods API
            AllowDirectDownloads = 2, // This game allows mods to be downloaded directly without API validation
        }

        // - Fields -
        [UnityEngine.SerializeField]
        protected API.GameObject _data;

        public int id                                   { get { return _data.id; } }
        public Status status                            { get { return (Status)_data.presentation_option; } }
        public User submittedBy                         { get; protected set; }
        public TimeStamp dateAdded                      { get; protected set; }
        public TimeStamp dateUpdated                    { get; protected set; }
        public TimeStamp dateLive                       { get; protected set; }
        public PresentationOption presentationOption    { get { return (PresentationOption)_data.presentation_option; } }
        public SubmissionOption submissionOption        { get { return (SubmissionOption)_data.submission_option; } }
        public CurationOption curationOption            { get { return (CurationOption)_data.curation_option; } }
        public CommunityOptions communityOptions        { get { return (CommunityOptions)_data.community_options; } }
        public RevenueOptions revenueOptions            { get { return (RevenueOptions)_data.revenue_options; } }
        public APIAccessOptions apiAccessOptions        { get { return (APIAccessOptions)_data.api_access_options; } }
        public string ugcName                           { get { return _data.ugc_name; } }
        public IconURLInfo icon                         { get; protected set; }
        public LogoURLInfo logo                         { get; protected set; }
        public HeaderImageURLInfo headerImage           { get; protected set; }
        public string homepage                          { get { return _data.homepage; } }
        public string name                              { get { return _data.name; } }
        public string nameId                            { get { return _data.name_id; } }
        public string summary                           { get { return _data.summary; } }
        public string instructions                      { get { return _data.instructions; } }
        public string profileURL                        { get { return _data.profile_url; } }
        public GameTagOption[] taggingOptions           { get; protected set; }
        
        // - IAPIObjectWrapper Interface -
        public void WrapAPIObject(API.GameObject apiObject)
        {
            this._data = apiObject;

            this.submittedBy = new User();
            this.submittedBy.WrapAPIObject(apiObject.submitted_by);
            this.dateAdded   = TimeStamp.GenerateFromServerTimeStamp(apiObject.date_added);
            this.dateUpdated = TimeStamp.GenerateFromServerTimeStamp(apiObject.date_updated);
            this.dateLive    = TimeStamp.GenerateFromServerTimeStamp(apiObject.date_live);
            this.icon        = new IconURLInfo();
            this.icon.WrapAPIObject(apiObject.icon);
            this.logo        = new LogoURLInfo();
            this.logo.WrapAPIObject(apiObject.logo);
            this.headerImage = new HeaderImageURLInfo();
            this.headerImage.WrapAPIObject(apiObject.header);

            int taggingOptionCount = (apiObject.tag_options == null ? 0 : apiObject.tag_options.Length);
            this.taggingOptions = new GameTagOption[taggingOptionCount];
            for(int i = 0;
                i < taggingOptionCount;
                ++i)
            {
                this.taggingOptions[i] = new GameTagOption();
                this.taggingOptions[i].WrapAPIObject(apiObject.tag_options[i]);
            }
        }

        public API.GameObject GetAPIObject()
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
            return this.Equals(obj as GameInfo);
        }

        public bool Equals(GameInfo other)
        {
            return (Object.ReferenceEquals(this, other)
                    || this._data.Equals(other._data));
        }
    }

    [Serializable]
    public class EditableGameInfo : GameInfo
    {
        public static EditableGameInfo FromGameInfo(GameInfo gameInfo)
        {
            EditableGameInfo newEGI = new EditableGameInfo();

            newEGI.WrapAPIObject(gameInfo.GetAPIObject());

            return newEGI;
        }

        // - Put Request Values -
        private Dictionary<string, string> putValues = new Dictionary<string, string>();
        public Dictionary<string, string> AsPutRequestValues()
        {
            return putValues;
        }

        // --- SETTERS ---
        // Status of a game. We recommend you never change this once you have accepted your game to be available via the API (see status and visibility for details):
        public void SetStatus(Status value)
        {
            UnityEngine.Debug.Assert(value == Status.Accepted || value == Status.NotAccepted, 
                                     "Status.Accepted and Status.NotAccepted are the only permittable values for SetStatus");
            
            _data.status = (int)value;

            putValues["status"] = ((int)value).ToString();
        }
        // Name of your game. Cannot exceed 80 characters.
        public void SetName(string value)
        {
            if(value.Length > 80)
            {
                value = value.Substring(0, 80);
                UnityEngine.Debug.LogWarning("GameInfo.name cannot exceed 80 characters. Truncating.");
            }

            _data.name = value;

            putValues["name"] = value;
        }
        // Subdomain for the game on mod.io. Highly recommended to not change this unless absolutely required. Cannot exceed 20 characters.
        public void SetNameId(string value)
        {
            if(value.Length > 20)
            {
                value = value.Substring(0, 20);
                UnityEngine.Debug.LogWarning("GameInfo.nameId cannot exceed 20 characters. Truncating.");
            }

            _data.name_id = value;

            putValues["name_id"] = value;
        }
        // Explain your games mod support in 1 paragraph. Cannot exceed 250 characters.
        public void SetSummary(string value)
        {
            if(value.Length > 250)
            {
                value = value.Substring(0, 250);
                UnityEngine.Debug.LogWarning("GameInfo.summary cannot exceed 250 characters. Truncating.");
            }

            _data.summary = value;   

            putValues["summary"] = value;
        }
        // Instructions and links creators should follow to upload mods. Keep it short and explain details like are mods submitted in-game or via tools you have created.
        public void SetInstructions(string value)
        {
            _data.instructions = value;

            putValues["instructions"] = value;
        }
        // Official homepage for your game. Must be a valid URL.
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
        // Word used to describe user-generated content (mods, items, addons etc).
        public void SetUGCName(string value)
        {
            _data.ugc_name = value; 

            putValues["ugc_name"] = value;
        }
        // Choose the presentation style you want on the mod.io website
        public void SetPresentationOption(GameInfo.PresentationOption value)
        {
            _data.presentation_option = (int)value;

            putValues["presentation_option"] = ((int)value).ToString();
        }
        // Choose the submission process you want modders to follow
        public void SetSubmissionOption(GameInfo.SubmissionOption value)
        {
            _data.submission_option = (int)value;

            putValues["submission_option"] = ((int)value).ToString();
        }
        // Choose the curation process your team follows to approve mods
        public void SetCurationOption(GameInfo.CurationOption value)
        {
            _data.curation_option = (int)value;

            putValues["curation_option"] = ((int)value).ToString();
        }
        // Choose the community features enabled on the mod.io website
        public void SetCommunityOptions(GameInfo.CommunityOptions value)
        {
            _data.community_options = (int)value;

            putValues["community_options"] = ((int)value).ToString();
        }
        // Choose the revenue capabilities mods can enable
        public void SetRevenueOptions(GameInfo.RevenueOptions value)
        {
            _data.revenue_options = (int)value;

            putValues["revenue_options"] = ((int)value).ToString();
        }
        // Choose the level of API access your game allows
        public void SetAPIAccessOptions(GameInfo.APIAccessOptions value)
        {
            _data.api_access_options = (int)value;

            putValues["api_access_options"] = ((int)value).ToString();
        }
    }
}
