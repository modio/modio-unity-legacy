using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModIO
{
    public enum LogoVersion
    {
        Original = 0,
        Thumb_320x180,
        Thumb_640x360,
        Thumb_1280x720
    }

    // --------- BASE API OBJECTS ---------
    [Serializable]
    public class APIMessage
    {
        public int code; // (int32)  HTTP status code of response.
        public string message; // The server response to your request. Responses will vary from endpoint but object structure will persist.
    }

    [Serializable]
    public class APIError
    {
        [Serializable]
        private class APIErrorObject
        {
            public int code = -1; // (int32)  HTTP code of the error.
            public string message = ""; // The server response to your request. Responses will vary from endpoint but object structure will persist.
        }
        [SerializeField]
        private APIErrorObject error = new APIErrorObject();

        public int code { get { return error.code; } set { error.code = value; } }
        public string message { get { return error.message; } set { error.message = value; } }
        public string url = "";
        public Dictionary<string, string> headers = new Dictionary<string, string>(0);

        public static APIError GenerateFromWebRequest(UnityEngine.Networking.UnityWebRequest webRequest)
        {
            Debug.Assert(webRequest.isNetworkError || webRequest.isHttpError);
            
            APIError retVal = new APIError();
            retVal.url = webRequest.url;

            if(webRequest.isNetworkError
               || webRequest.responseCode == 404)
            {
                retVal.code = (int)webRequest.responseCode;
                retVal.message = webRequest.error;
                retVal.headers = new Dictionary<string, string>();
            }
            else // if(webRequest.isHttpError)
            {
                APIError error = JsonUtility.FromJson<APIError>(webRequest.downloadHandler.text);
                retVal.code = error.code;
                retVal.message = error.message;
                retVal.headers = webRequest.GetResponseHeaders();
            }

            return retVal;
        }
    }

    [Serializable]
    public class APIObjectArray<T>
    {
        // - API snake_case fields -
        [SerializeField] private int result_count;
        [SerializeField] private int result_limit;
        [SerializeField] private int result_offset;

        // - Unity named fields -
        public T[] data = null;
        public int resultCount { get { return result_count; } set { result_count = value; } }
        public int resultLimit { get { return result_limit; } set { result_limit = value; } }
        public int resultOffset { get { return result_offset; } set { result_offset = value; } }
    }

    // --------- API DATA OBJECTS ---------
    [Serializable]
    public class AuthenticationData
    {
        // - API snake_case fields -
        [SerializeField]
        private string access_token;
        
        // - Unity named fields -
        public string accessToken { get { return access_token; } set { access_token = value; } }
    }

    [Serializable]
    public class Avatar
    {
        public string filename; // Image filename, including file extension.
        public string original; // Full URL to the image.
        public string thumb_50x50; // URL to the small thumbnail image.
        public string thumb_100x100;   // URL to the medium thumbnail image.
    }

    [Serializable]
    public class Comment
    {
        // - API snake_case fields -
        [SerializeField] private int id;
        [SerializeField] private int mod_id;
        [SerializeField] private User submitted_by;
        [SerializeField] private int date_added;
        [SerializeField] private int reply_id;
        [SerializeField] private string reply_position;
        [SerializeField] private int karma_guest;

        // - Unity named fields -
        public int ID // (int32)  Unique id of the comment.
        {
            get { return id; }
            set { id = value; }
        }
        public int modID // (int32)  Unique id of the parent mod.
        {
            get { return mod_id; }
            set { mod_id = value; }
        }
        public User submittedBy // Contains user data.
        {
            get { return submitted_by; }
            set { submitted_by = value; }
        }
        public int replyID // (int32)  Unique id of the reply used to submitting a nested reply to the published comment.
        {
            get { return reply_id; }
            set { reply_id = value; }
        }
        public string replyPosition // Nesting position of the reply.
        {
            get { return reply_position; }
            set { reply_position = value; }
        }
        public int karma; // (int32)  The amount of karma the comment has received.
        public int karmaGuest // (int32)  The amount of karma received by guests.
        {
            get { return karma_guest; }
            set { karma_guest = value; }
        }
        public string summary; // The displayed comment.

        // - Accessor Functions -
        public TimeStamp GetDateAdded()
        {
            return TimeStamp.GenerateFromServerTimeStamp(date_added);
        }
    }

    [Serializable]
    public class MetadataKVP
    {
        public string metakey; // The key of the key-value pair.
        public string metavalue; // The value of the key-value pair.
    }

    [Serializable]
    public class FieldChange
    {
        public string field; // The field of the changed value.
        public string before; // The value prior to the event.
        public string after; // The newly-updated value.
    }

    [Serializable]
    public class Filehash
    {
        public string md5; // MD5 filehash.
    }

    [Serializable]
    public class Game
    {
        // - Enum -
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
            Disabled = 0,
            Restricted = 1, // This game allows 3rd parties to access the mods API
            Unrestricted = 2, // This game allows mods to be downloaded directly without API validation
        }


        // - API snake_case fields -
        [SerializeField] private int id;
        [SerializeField] private int status;
        [SerializeField] private User submitted_by;
        [SerializeField] private int date_added;
        [SerializeField] private int date_updated;
        [SerializeField] private int date_live;
        [SerializeField] private int presentation_options;
        [SerializeField] private int submission_options;
        [SerializeField] private int curation_options;
        [SerializeField] private int community_options;
        [SerializeField] private int revenue_options;
        [SerializeField] private int api_options;
        [SerializeField] private string ugc_name;
        [SerializeField] private string name_id;
        [SerializeField] private string profile_url;
        [SerializeField] private GameTagOption[] tag_options;

        // - Unity named fields -
        public int ID // Unique game id.
        {
            get { return id; }
            set { id = value; }
        }
        public User submittedBy // Contains user data.
        {
            get { return submitted_by; }
            set { submitted_by = value; }
        }
        public string UGCName // Singular string that best describes the type of user-generated content.
        {
            get { return ugc_name; }
            set { ugc_name = value; }
        }
        public Icon icon; // Contains icon data.
        public Logo logo; // Contains logo data.
        public HeaderImage header; // Contains header data.
        public string homepage; // Official game website URL.
        public string name; // Title of the game.
        public string nameID // The unique SEO friendly URL of the game.
        {
            get { return name_id; }
            set { name_id = value; }
        }
        public string summary; // Brief summary of the game.
        public string instructions; // Modding instructions for developers.
        public string profileUrl // website url for the game.
        {
            get { return profile_url; }
            set { profile_url = value; }
        }
        public GameTagOption[] tagOptions // Contains categories data.
        {
            get { return tag_options; }
            set { tag_options = value; }
        }

        // - Accessor Functions -
        public TimeStamp GetDateAdded()
        {
            return TimeStamp.GenerateFromServerTimeStamp(date_added);
        }
        public TimeStamp GetDateUpdated()
        {
            return TimeStamp.GenerateFromServerTimeStamp(date_updated);
        }
        public TimeStamp GetDateLive()
        {
            return TimeStamp.GenerateFromServerTimeStamp(date_live);
        }

        public Status GetStatus()
        {
            return (Status)status;
        }
        public void SetStatus(Status value)
        {
            status = (int)value;
        }
        public PresentationOption GetPresentationOption()
        {
            return (PresentationOption)presentation_options;
        }
        public void SetPresentationOption(PresentationOption value)
        {
            presentation_options = (int)value;
        }
        public SubmissionOption GetSubmissionOption()
        {
            return (SubmissionOption)submission_options;
        }
        public void SetSubmissionOption(SubmissionOption value)
        {
            submission_options = (int)value;
        }
        public CurationOption GetCurationOption()
        {
            return (CurationOption)curation_options;
        }
        public void SetCurationOption(CurationOption value)
        {
            curation_options = (int)value;
        }
        public CommunityOptions GetCommunityOptions()
        {
            return (CommunityOptions)community_options;
        }
        public void SetCommunityOptions(CommunityOptions value)
        {
            community_options = (int)value;
        }
        public RevenueOptions GetRevenueOptions()
        {
            return (RevenueOptions)revenue_options;
        }
        public void SetRevenueOptions(RevenueOptions value)
        {
            revenue_options = (int)value;
        }
        public APIAccessOptions GetAPIAccessOptions()
        {
            return (APIAccessOptions)api_options;
        }
        public void SetAPIAccessOptions(APIAccessOptions value)
        {
            api_options = (int)value;
        }
    }

    [Serializable]
    public class GameTagOption
    {
        // - API snake_case fields -
        [SerializeField] private int admin_only;

        // - Unity named fields -
        public string name; // The name of the category.
        public string type; // Are tags selected via checkboxes or a single dropdown.
        public int adminOnly // Is this an admin only tag? If so only admin's can see this category and it can be used for filtering.
        {
            get { return admin_only; }
            set { admin_only = value; }
        }
        public string[] tags; // Eligible tags for this game.
    }

    [Serializable]
    public class HeaderImage
    {
        public string filename; // Image filename, with file extension included.
        public string original; // URL to the full-sized header image.
    }

    [Serializable]
    public class Icon
    {
        public string filename; // Image filename, with file extension included.
        public string original; // URL to full-sized image.
        public string thumb_64x64; // URL to the small thumbnail image.
        public string thumb_128x128; // URL to the medium thumbnail image.
        public string thumb_256x256; // URL to the large thumbnail image.
    }

    [Serializable]
    public class ImageData
    {
        public string original; // URL to the full image.
        public string thumbnail; // URL to the thumbnail image.
        public string filename; // Image filename, with the extension included.
    }

    [Serializable]
    public class Logo
    {
        public string filename; // Image filename, with file extension included.
        public string original; // URL to full-sized image.
        public string thumb_320x180; // URL to small thumbnail image.
        public string thumb_640x360; // URL to medium thumbnail image.
        public string thumb_1280x720; // URL to large thumbnail image.
    }

    [Serializable]
    public class Mod
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

        // - API snake_case fields -
        [SerializeField] private int id;
        [SerializeField] private int game_id;
        [SerializeField] private int status;
        [SerializeField] private int visible;
        [SerializeField] private User submitted_by;
        [SerializeField] private int date_added;
        [SerializeField] private int date_updated;
        [SerializeField] private int date_live;
        [SerializeField] private string name_id;
        [SerializeField] private string metadata_blob;
        [SerializeField] private string profile_url;
        [SerializeField] private RatingSummary rating_summary;

        // - Unity named fields -
        public int ID // Unique mod id.
        {
            get { return id; }
            set { id = value; }
        }
        public int gameID // Unique game id.
        {
            get { return game_id; }
            set { game_id = value; }
        }
        public User submittedBy // Contains user data.
        {
            get { return submitted_by; }
            set { submitted_by = value; }
        }
        public Logo logo; // Contains logo data.
        public string homepage; // Mod homepage URL.
        public string name; // Name of the mod.
        public string nameID // Unique SEO-friendly mod uri.
        {
            get { return name_id; }
            set { name_id = value; }
        }
        public string summary; // Brief summary of the mod.
        public string description; // Description of the mod.
        public string metadataBlob // Comma-separated metadata for the mod.
        {
            get { return metadata_blob; }
            set { metadata_blob = value; }
        }
        public string profileUrl // Official website url for the mod.
        {
            get { return profile_url; }
            set { profile_url = value; }
        }
        public Modfile modfile; // Contains file data.
        public object media; // Contains media data.
        public RatingSummary ratingSummary // Contains ratings data.
        {
            get { return rating_summary; }
            set { rating_summary = value; }
        }
        public ModTag[] tags; // Contains Mod Tag data.

        // - Accessor Functions -
        public TimeStamp GetDateAdded()
        {
            return TimeStamp.GenerateFromServerTimeStamp(date_added);
        }
        public TimeStamp GetDateUpdated()
        {
            return TimeStamp.GenerateFromServerTimeStamp(date_updated);
        }
        public TimeStamp GetDateLive()
        {
            return TimeStamp.GenerateFromServerTimeStamp(date_live);
        }

        public Status GetStatus()
        {
            return (Status)status;
        }
        public void SetStatus(Status value)
        {
            status = (int)value;
        }
        public Visibility GetVisible()
        {
            return (Visibility)visible;
        }
        public void SetVisible(Visibility value)
        {
            visible = (int)value;
        }

        public string[] GetTagNames()
        {
            string[] retVal = new string[tags.Length];
            for(int i = 0;
                i < tags.Length;
                ++i)
            {
                retVal[i] = tags[i].name;
            }
            return retVal;
        }
    }

    [Serializable]
    public class ModDependency
    {
        // - API snake_case fields -
        [SerializeField] private int mod_id;
        [SerializeField] private int date_added;

        // - Unity named fields -
        public int modID // (int32)  Unique id of the mod that is the dependency.
        {
            get { return mod_id; }
            set { mod_id = value; }
        }

        // - Accessor Functions -
        public TimeStamp GetDateAdded()
        {
            return TimeStamp.GenerateFromServerTimeStamp(date_added);
        }
    }

    [Serializable]
    public class ModEvent
    {
        // - Enums -
        public enum EventType
        {
            ModVisibilityChange,
            ModLive,
            ModfileChange,
        }

        private static Dictionary<string, EventType> eventNameMap;
        static ModEvent()
        {
            eventNameMap = new Dictionary<string, EventType>();

            eventNameMap["MODFILE_CHANGE"] = EventType.ModfileChange;
            eventNameMap["MOD_VISIBILITY_CHANGE"] = EventType.ModVisibilityChange;
            eventNameMap["MOD_LIVE"] = EventType.ModLive;
        }

        public static string GetNameForType(EventType value)
        {
            foreach(KeyValuePair<string, EventType> kvp in eventNameMap)
            {
                if(kvp.Value == value)
                {
                    return kvp.Key;
                }
            }
            Debug.LogError("EventType \'" + value.ToString() + "\' has no corresponding name entry in ModEvent.eventNameMap");
            return "";
        }

        // - API snake_case fields -
        [SerializeField] private int id;
        [SerializeField] private int mod_id;
        [SerializeField] private int user_id;
        [SerializeField] private int date_added;
        [SerializeField] private string event_type;

        // - Unity named fields -
        public int ID // Unique id of activity record.
        {
            get { return id; }
            set { id = value; }
        }
        public int modID // Unique id of the parent mod.
        {
            get { return mod_id; }
            set { mod_id = value; }
        }
        public int userID // Unique id of the user who triggered the action.
        {
            get { return user_id; }
            set { user_id = value; }
        }
        public FieldChange[] changes; // Contains all changes for the event.

        // - Accessor Functions -
        public TimeStamp GetDateAdded()
        {
            return TimeStamp.GenerateFromServerTimeStamp(date_added);
        }

        public EventType GetEventType()
        {
            return eventNameMap[event_type];
        }
        public void SetEventType(EventType value)
        {
            event_type = ModEvent.GetNameForType(value);
        }
    }

    [Serializable]
    public class Modfile
    {
        // - API snake_case fields -
        [SerializeField] private int id;
        [SerializeField] private int mod_id;
        [SerializeField] private int date_added;
        [SerializeField] private int date_scanned;
        [SerializeField] private int virus_status;
        [SerializeField] private int virus_positive;
        [SerializeField] private string metadata_blob;
        [SerializeField] private string download_url;
        
        // - Unity named fields -
        public int ID // (int32)  Unique file id.
        {
            get { return id; }
            set { id = value; }
        }
        public int modID // (int32)  Unique mod id.
        {
            get { return mod_id; }
            set { mod_id = value; }
        }
        public int virusStatus // (int32)  The status of the virus scan for the file.
        {
            get { return virus_status; }
            set { virus_status = value; }
        }
        public int virusPositive // (int32)  Has the file been positively flagged as a virus?
        {
            get { return virus_positive; }
            set { virus_positive = value; }
        }
        public int filesize; // (int32)  Size of the file in bytes.
        public Filehash filehash; // Contains filehashes for file.
        public string filename; // Name of the file including file extension.
        public string version; // The release version this file represents.
        public string virustotal; // Text output from virustotal scan.
        public string changelog; // List of all changes in this file release.
        public string metadataBlob // Metadata stored by the game developer for this file.
        {
            get { return metadata_blob; }
            set { metadata_blob = value; }
        }
        public string downloadURL // Link to download the file from the mod.io CDN.
        {
            get { return download_url; }
            set { download_url = value; }
        }

        // - Accessor Functions -
        public TimeStamp GetDateAdded()
        {
            return TimeStamp.GenerateFromServerTimeStamp(date_added);
        }
        public TimeStamp GetDateScanned()
        {
            return TimeStamp.GenerateFromServerTimeStamp(date_scanned);
        }
    }

    [Serializable]
    public class ModTag
    {
        // - API snake_case fields -
        [SerializeField] private int date_added;

        // - Unity named fields -
        public string name; // The displayed tag.

        // - Accessor Functions -
        public TimeStamp GetDateAdded()
        {
            return TimeStamp.GenerateFromServerTimeStamp(date_added);
        }
    }

    [Serializable]
    public class RatingSummary
    {
        // - API snake_case fields -
        [SerializeField] private int total_ratings;
        [SerializeField] private int positive_ratings;
        [SerializeField] private int negative_ratings;
        [SerializeField] private float weighted_aggregate;
        [SerializeField] private int percentage_positive;
        [SerializeField] private string display_text;

        // - Unity named fields -
        public int totalRatings // (int32)  Total ratings count.
        {
            get { return total_ratings; }
            set { total_ratings = value; }
        }
        public int positiveRatings // (int32)  Positive ratings count.
        {
            get { return positive_ratings; }
            set { positive_ratings = value; }
        }
        public int negativeRatings // (int32)  Negative ratings count.
        {
            get { return negative_ratings; }
            set { negative_ratings = value; }
        }
        public float weightedAggregate // Weighted rating taking into account positive & negative ratings.
        {
            get { return weighted_aggregate; }
            set { weighted_aggregate = value; }
        }
        public int percentagePositive // (int32)  Rating of the mod as a percentage.
        {
            get { return percentage_positive; }
            set { percentage_positive = value; }
        }
        public string displayText // Text representation of the rating total.
        {
            get { return display_text; }
            set { display_text = value; }
        }
    }

    [Serializable]
    public class TeamMember
    {
        // - Enums -
        public enum PermissionLevel
        {
            Guest = 0,
            Member = 1,
            Contributor = 2,
            Manager = 4,
            Leader = 8,
        }

        // - API snake_case fields -
        [SerializeField] private int id;
        [SerializeField] private int date_added;
        [SerializeField] private int level;

        // - Unity named fields -
        public int ID // (int32)  Unique access id.
        {
            get { return id; }
            set { id = value; }
        }
        public User user; // Contains user data.
        public string position; // Custom title, has no effect on any access rights.

        // - Accessor Functions -
        public TimeStamp GetDateAdded()
        {
            return TimeStamp.GenerateFromServerTimeStamp(date_added);
        }

        public PermissionLevel GetPermissionLevel()
        {
            return (PermissionLevel)level;
        }
        public void SetPermissionLevel(PermissionLevel value)
        {
            level = (int)value;
        }
    }

    [Serializable]
    public class User
    {
        // - API snake_case fields -
        [SerializeField] private int id;
        [SerializeField] private string name_id;
        [SerializeField] private int date_online;
        [SerializeField] private string profile_url;

        // - Unity named fields -
        public int ID // (int32)  Unique id of the user.
        {
            get { return id; }
            set { id = value; }
        }
        public string nameID // Unique nameid of user which forms end of their profile URL.
        {
            get { return name_id; }
            set { name_id = value; }
        }
        public string username; // Non-unique username of the user.
        public Avatar avatar; // Contains avatar data.
        public string timezone; // The Timezone of the user, shown in {Country}/{City} format.
        public string language; // The users language preference, limited to two characters.
        public string profileURL // URL to the user profile.
        {
            get { return profile_url; }
            set { profile_url = value; }
        }

        // - Accessor Functions -
        public TimeStamp GetDateOnline()
        {
            return TimeStamp.GenerateFromServerTimeStamp(date_online);
        }
    }
}
