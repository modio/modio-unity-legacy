using System;

namespace ModIO
{
    [Serializable]
    public class GameInfo : IEquatable<GameInfo>, IAPIObjectWrapper<API.GameObject>
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
        private API.GameObject _data;

        public int id                                   { get { return _data.id; } }
        public Status status                            { get { return (Status)_data.presentation_option; } }
        public User submittedBy                         { get; private set; }
        public TimeStamp dateAdded                      { get; private set; }
        public TimeStamp dateUpdated                    { get; private set; }
        public TimeStamp dateLive                       { get; private set; }
        public PresentationOption presentationOption    { get { return (PresentationOption)_data.presentation_option; } }
        public SubmissionOption submissionOption        { get { return (SubmissionOption)_data.submission_option; } }
        public CurationOption curationOption            { get { return (CurationOption)_data.curation_option; } }
        public CommunityOptions communityOptions        { get { return (CommunityOptions)_data.community_options; } }
        public RevenueOptions revenueOptions            { get { return (RevenueOptions)_data.revenue_options; } }
        public APIAccessOptions apiAccessOptions        { get { return (APIAccessOptions)_data.api_access_options; } }
        public string ugcName                           { get { return _data.ugc_name; } }
        public IconURLInfo icon                         { get; private set; }
        public LogoURLInfo logo                         { get; private set; }
        public HeaderImageURLInfo headerImage           { get; private set; }
        public string homepage                          { get { return _data.homepage; } }
        public string name                              { get { return _data.name; } }
        public string nameId                            { get { return _data.name_id; } }
        public string summary                           { get { return _data.summary; } }
        public string instructions                      { get { return _data.instructions; } }
        public string profileURL                        { get { return _data.profile_url; } }
        public GameTagOption[] taggingOptions           { get; private set; }
        
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

            this.taggingOptions = new GameTagOption[apiObject.tag_options.Length];
            for(int i = 0;
                i < apiObject.tag_options.Length;
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
}
