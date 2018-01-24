using System;

namespace ModIO
{
    [Serializable]
    public class GameInfo : IEquatable<GameInfo>
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

        // - Constructors - 
        public static GameInfo GenerateFromAPIObject(API.GameObject apiObject)
        {
            GameInfo newGame = new GameInfo();
            newGame._data = apiObject;

            newGame.submittedBy = User.GenerateFromAPIObject(apiObject.submitted_by);
            newGame.dateAdded   = TimeStamp.GenerateFromServerTimeStamp(apiObject.date_added);
            newGame.dateUpdated = TimeStamp.GenerateFromServerTimeStamp(apiObject.date_updated);
            newGame.dateLive    = TimeStamp.GenerateFromServerTimeStamp(apiObject.date_live);
            newGame.icon        = IconInfo.GenerateFromAPIObject(apiObject.icon);
            newGame.logo        = LogoURLInfo.GenerateFromAPIObject(apiObject.logo);
            newGame.headerImage = HeaderImageInfo.GenerateFromAPIObject(apiObject.header);

            newGame.taggingOptions = new GameTagOption[apiObject.tag_options.Length];
            for(int i = 0;
                i < apiObject.tag_options.Length;
                ++i)
            {
                newGame.taggingOptions[i] = GameTagOption.GenerateFromAPIObject(apiObject.tag_options[i]);
            }

            return newGame;
        }

        public static GameInfo[] GenerateFromAPIObjectArray(API.GameObject[] apiObjectArray)
        {
            GameInfo[] objectArray = new GameInfo[apiObjectArray.Length];

            for(int i = 0;
                i < apiObjectArray.Length;
                ++i)
            {
                objectArray[i] = GameInfo.GenerateFromAPIObject(apiObjectArray[i]);
            }

            return objectArray;
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
        public IconInfo icon                            { get; private set; }
        public LogoURLInfo logo                            { get; private set; }
        public HeaderImageInfo headerImage              { get; private set; }
        public string homepage                          { get { return _data.homepage; } }
        public string name                              { get { return _data.name; } }
        public string nameId                            { get { return _data.name_id; } }
        public string summary                           { get { return _data.summary; } }
        public string instructions                      { get { return _data.instructions; } }
        public string profileURL                        { get { return _data.profile_url; } }
        public GameTagOption[] taggingOptions           { get; private set; }

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
