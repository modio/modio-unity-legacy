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
        public int code; // (int32)  HTTP code of the error.
        public string message; // The server response to your request. Responses will vary from endpoint but object structure will persist.
    }

    [Serializable]
    public class APIObjectArray<T>
    {
        // - API snake_case fields -
        [SerializeField] private int cursor_id = -1;
        [SerializeField] private int prev_id = -1;
        [SerializeField] private int next_id = -1;
        [SerializeField] private int result_count = 0;

        // - Unity named fields -
        public T[] data = null;
        public int cursorID { get { return cursor_id; } set { cursor_id = value; } }
        public int previousID { get { return prev_id; } set { prev_id = value; } }
        public int nextID { get { return next_id; } set { next_id = value; } }
        public int resultCount { get { return result_count; } set { result_count = value; } }
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
        public int dateAdded // (int32)  Unix timestamp of when the comment was published.
        {
            get { return date_added; }
            set { date_added = value; }
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
    }

    [Serializable]
    public class MetadataKVP
    {
        public string key; // The key of the key-value pair.
        public string value; // The value of the key-value pair.
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
        // - API snake_case fields -
        [SerializeField] private int id;
        [SerializeField] private User submitted_by;
        [SerializeField] private int date_added;
        [SerializeField] private int date_updated;
        [SerializeField] private int date_live;
        [SerializeField] private int api;
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
        public int dateAdded // Unix timestamp of date registered.
        {
            get { return date_added; }
            set { date_added = value; }
        }
        public int dateUpdated // Unix timestamp of date updated.
        {
            get { return date_updated; }
            set { date_updated = value; }
        }
        public int dateLive // Unix timestamp of when game was set live.
        {
            get { return date_live; }
            set { date_live = value; }
        }
        public int presentation; // Determines which presentation style you want to use for your game on the mod.io website
        public int community; // Determines the rights community members have with the game.
        public int submission; // Determines the submission process you want modders to follow.
        public int curation; // Determines the curation process for the game.
        public int revenue; // Bitwise. Determines the revenue capabilities for mods of the game. For selecting multiple options you need to submit the bitwise value. i.e. If you want to allow user-generated content to be sold(1), to receive donations(2) and allow them to control their supply and scarcity(8) your would submit 11 (8 + 2 + 1).
        public int API // Determines what permissions you want to enable via the mod.io API.
        {
            get { return api; }
            set { api = value; }
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

        #region --- FieldNotes ---
            /***
            *   .presentation
            *       0 = Grid View: Displays mods in a grid (visual but less informative, default setting)
            *       1 = Table View: Displays mods in a table (easier to browse).
            *   .community
            *       0 = Discussion board disabled, community cannot share guides and news
            *       1 = Discussion Board enabled only
            *       2 = Community can only share guides and news
            *       3 = Discussion Board enabled and community can share news and guides
            *   .submission
            *       0 = Control the upload process. You will have to build an upload system
            *           either in-game or via a standalone app, which enables developers to
            *           submit mods to the tags you have configured. Because you control the
            *           flow, you can pre-validate and compile mods, to ensure they will work
            *           in your game. In the long run this option will save you time as you
            *           can accept more submissions, but it requires more setup to get running
            *           and isn't as open as the above option.
            *           NOTE: mod profiles can still be created online, but uploads will have
            *           to occur via the tools you supply.
            *       1 = Enable mod uploads from anywhere. Allow developers to upload mods via
            *           the website and API, and pick the tags their mod is built for. No
            *           validation will be done on the files submitted, it will be the
            *           responsibility of your game and apps built to process the mods
            *           installation based on the tags selected and determine if the mod is
            *           valid and works. For example a mod might be uploaded to the 'map' tag.
            *           When a user subscribes to this mod, your game will need to verify it
            *           contains a map file and install it where maps are located. If this
            *           fails, your game or the community will have to flag the mod as
            *           'incompatible' to remove it from the listing.
            *   .curation
            *       0 = Mods are immediately available to play, without any intervention or
            *           work from your team.
            *       1 = Screen only mods the author wants to sell, before they are available to
            *           purchase via the API.
            *       2 = All mods must be accepted by someone on your team. This option is useful
            *           for games that have a small number of mods and want to control the
            *           experience, or you need to set the parameters attached to a mod
            *           (i.e. a weapon may require the rate of fire, power level, clip size etc).
            *           It can also be used for complex mods, which you may need to build into
            *           your game or distribute as DLC.
            *   .revenue
            *       1 = Allow user-generated content to be sold
            *       2 = Allow user-generated content to receive donations
            *       4 = Allow user-generated content to be traded (not subject to revenue share)
            *       8 = Allow user-generated content to control supply and scarcity.
            *   .api
            *       0 = Third parties cannot access your mods API and mods cannot be downloaded
            *           directly without API validation.
            *       1 = Allow 3rd parties to access your mods API (recommended, an open API will
            *           encourage a healthy ecosystem of tools and apps) but mods cannot be
            *           downloaded directly
            *       2 = Allow mods to be downloaded directly but 3rd parties cannot access your
            *           mods API.
            *       3 = Allow third parties to access your mods API and allow mods to be
            *           downloaded directly without api validation.
            ***/
        #endregion
    }

    [Serializable]
    public class GameActivity
    {
        // - API snake_case fields -
        [SerializeField] private int id;
        [SerializeField] private int game_id;
        [SerializeField] private int user_id;
        [SerializeField] private string date_added;
        [SerializeField] private string _event;

        // - Unity named fields -
        public int ID // Unique id of activity record.
        {
            get { return id; }
            set { id = value; }
        }
        public int gameID // Unique id of the parent game.
        {
            get { return game_id; }
            set { game_id = value; }
        }
        public int userID // Unique id of the user who triggered the action.
        {
            get { return user_id; }
            set { user_id = value; }
        }
        public string dateAdded // Unix timestamp of when the event occured.
        {
            get { return date_added; }
            set { date_added = value; }
        }
        public FieldChange[] changes; // Contains all changes for the event.
        public string eventType // Type of event the activity was. ie. GAME_UPDATE or GAME_VISIBILITY_CHANGE.
        {
            get { return _event; }
            set { _event = value; }
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
        public string thumb_320x180; // URL to small thumbnail image.
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
        // - API snake_case fields -
        [SerializeField] private int id;
        [SerializeField] private int game_id;
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
        public int gameID //Unique game id.
        {
            get { return game_id; }
            set { game_id = value; }
        }
        public User submittedBy // Contains user data.
        {
            get { return submitted_by; }
            set { submitted_by = value; }
        }
        public int dateAdded // Unix timestamp of date registered.
        {
            get { return date_added; }
            set { date_added = value; }
        }
        public int dateUpdated // Unix timestamp of date last updated.
        {
            get { return date_updated; }
            set { date_updated = value; }
        }
        public int dateLive // (int32)  Unix timestamp of date mod was set live.
        {
            get { return date_live; }
            set { date_live = value; }
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

        // - Unity named fields -
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
        public int dateAdded // (int32)  Unix timestamp of when the dependency was added.
        {
            get { return date_added; }
            set { date_added = value; }
        }
    }

    [Serializable]
    public class ModActivity
    {
        // - API snake_case fields -
        [SerializeField] private int id;
        [SerializeField] private int mod_id;
        [SerializeField] private int user_id;
        [SerializeField] private string date_added;
        [SerializeField] private string _event;

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
        public string dateAdded // Unix timestamp of when the event occured.
        {
            get { return date_added; }
            set { date_added = value; }
        }
        public FieldChange[] changes; // Contains all changes for the event.
        public string eventType // Type of event the activity was. ie. GAME_UPDATE or GAME_VISIBILITY_CHANGE.
        {
            get { return _event; }
            set { _event = value; }
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
        public int dateAdded // (int32)  Unix timestamp of file upload time.
        {
            get { return date_added; }
            set { date_added = value; }
        }
        public int dateScanned // (int32)  Unix timestamp of file virus scan.
        {
            get { return date_scanned; }
            set { date_scanned = value; }
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
        public string downloadURL // Link to download the file from the mod.io CDN.
        {
            get { return download_url; }
            set { download_url = value; }
        }
    }

    [Serializable]
    public class ModTag
    {
        // - API snake_case fields -
        [SerializeField] private int date_added;

        // - Unity named fields -
        public string name; // The displayed tag.
        public int dateAdded // (int32)  Unix timestamp of when tag was applied.
        {
            get { return date_added; }
            set { date_added = value; }
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
    public class TeamMember // Access
    {
        // - API snake_case fields -
        [SerializeField] private int id;
        [SerializeField] private int date_added;

        // - Unity named fields -
        public int ID // (int32)  Unique access id.
        {
            get { return id; }
            set { id = value; }
        }
        public User user; // Contains user data.
        public int level; // (int32)  The level of permissions the member has within the team.
        public int dateAdded // (int32)  Unix timestamp of date the member joined the team.
        {
            get { return date_added; }
            set { date_added = value; }
        }
        public string position; // Custom title, has no effect on any access rights.

        #region --- Field Notes ---
            /***
            * .type
            *   0 = Guest
            *   1 = Member
            *   2 = Contributor
            *   4 = Manager
            *   8 = Leader
            ***/
        #endregion
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
        public int dateOnline // (int32)  Unix timestamp of when the user was last online.
        {
            get { return date_online; }
            set { date_online = value; }
        }
        public Avatar avatar; // Contains avatar data.
        public string timezone; // The Timezone of the user, shown in {Country}/{City} format.
        public string language; // The users language preference, limited to two characters.
        public string profileURL // URL to the user profile.
        {
            get { return profile_url; }
            set { profile_url = value; }
        }
    }
}
