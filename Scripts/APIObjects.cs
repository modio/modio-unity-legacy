using System;

// NOTE(@jackson): Currently using GetHashCode() to check for Array equality
namespace ModIO.API
{
    // --------- BASE API OBJECTS ---------
    [Serializable]
    public struct MessageObject
    {
        public int code;    // HTTP status code of response.
        public string message; // The server response to your request. Responses will vary depending on the endpoint, but the object structure will persist.
    }

    [Serializable]
    public struct ErrorObject
    {
        [Serializable]
        public struct InternalErrorObject
        {
            public int code;    // HTTP code of the error.
            public string message; // The server response to your request. Responses will vary depending on the endpoint, but the object structure will persist.
        }

        public InternalErrorObject error;
    }

    [Serializable]
    public struct ObjectArray<T>
    {
        public int result_count;    // Number of results returned in the current request.
        public int result_limit;    // Maximum number of results returned. Defaults to 100 unless overridden by _limit.
        public int result_offset;   // Number of results skipped over. Defaults to 1 unless overridden by _offset.
        public T[] data; // Contains all data returned from the request
    }

    // --------- API DATA OBJECTS ---------
    [Serializable]
    public struct AccessTokenObject
    {
        public string access_token; // OAuthToken that is assigned to the user for your game
    }

    [Serializable]
    public struct AvatarObject : IEquatable<AvatarObject>
    {
        public string filename; // Avatar filename including extension.
        public string original; // URL to the full-sized avatar.
        public string thumb_50x50; // URL to the small thumbnail image.
        public string thumb_100x100; // URL to the medium thumbnail image.

        // - Equality Operators -
        public override int GetHashCode()
        {
            return this.filename.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return (obj is AvatarObject
                    && this.Equals((AvatarObject)obj));
        }

        public bool Equals(AvatarObject other)
        {
            return(this.filename.Equals(other.filename)
                   && this.original.Equals(other.original)
                   && this.thumb_50x50.Equals(other.thumb_50x50)
                   && this.thumb_100x100.Equals(other.thumb_100x100));
        }
    }

    [Serializable]
    public struct CommentObject : IEquatable<CommentObject>
    {
        public int id;  // Unique id of the comment.
        public int mod_id;  // Unique id of the parent mod.
        public UserObject submitted_by; // Contains user data.
        public int date_added;  // Unix timestamp of date the comment was posted.
        public int reply_id;    // Id of the parent comment this comment is replying to (can be 0 if the comment is not a reply).
        public string reply_position; // Levels of nesting in a comment thread. How it works:
        public int karma;   // Karma received for the comment (can be postive or negative).
        public int karma_guest; // Karma received for guest comments (can be postive or negative).
        public string content; // Contents of the comment.

        // - Equality Operators -
        public override int GetHashCode()
        {
            return this.id;
        }

        public override bool Equals(object obj)
        {
            return (obj is CommentObject
                    && this.Equals((CommentObject)obj));
        }

        public bool Equals(CommentObject other)
        {
            return(this.id.Equals(other.id)
                   && this.mod_id.Equals(other.mod_id)
                   && this.submitted_by.Equals(other.submitted_by)
                   && this.date_added.Equals(other.date_added)
                   && this.reply_id.Equals(other.reply_id)
                   && this.reply_position.Equals(other.reply_position)
                   && this.karma.Equals(other.karma)
                   && this.karma_guest.Equals(other.karma_guest)
                   && this.content.Equals(other.content));
        }
    }

    [Serializable]
    public struct MetadataKVPObject : IEquatable<MetadataKVPObject>
    {
        public string metakey; // The key of the key-value pair.
        public string metavalue; // The value of the key-value pair.

        // - Equality Operators -
        public override int GetHashCode()
        {
            return this.metakey.GetHashCode() ^ this.metavalue.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return (obj is MetadataKVPObject
                    && this.Equals((MetadataKVPObject)obj));
        }

        public bool Equals(MetadataKVPObject other)
        {
            return(this.metakey.Equals(other.metakey)
                   && this.metavalue.Equals(other.metavalue));
        }
    }

    [Serializable]
    public struct FilehashObject : IEquatable<FilehashObject>
    {
        public string md5; // MD5 hash of the file.

        // - Equality Operators -
        public override int GetHashCode()
        {
            return this.md5.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return (obj is FilehashObject
                    && this.Equals((FilehashObject)obj));
        }

        public bool Equals(FilehashObject other)
        {
            return(this.md5.Equals(other.md5));
        }
    }

    [Serializable]
    public struct GameObject : IEquatable<GameObject>
    {
        public int id;  // Unique game id.
        public int status;  // Status of the game (see status and visibility for details):
        public UserObject submitted_by; // Contains user data.
        public int date_added;  // Unix timestamp of date game was registered.
        public int date_updated;    // Unix timestamp of date game was updated.
        public int date_live;   // Unix timestamp of date game was set live.
        public int presentation_option; // Presentation style used on the mod.io website:
        public int submission_option;   // Submission process modders must follow:
        public int curation_option; // Curation process used to approve mods:
        public int community_options;   // Community features enabled on the mod.io website:
        public int revenue_options; // Revenue capabilities mods can enable:
        public int api_access_options;  // Level of API access allowed by this game:
        public string ugc_name; // Word used to describe user-generated content (mods, items, addons etc).
        public IconObject icon; // Contains icon data.
        public LogoObject logo; // Contains logo data.
        public HeaderImageObject header; // Contains header data.
        public string homepage; // Official homepage of the game.
        public string name; // Name of the game.
        public string name_id; // Subdomain for the game on mod.io.
        public string summary; // Summary of the game.
        public string instructions; // A guide about creating and uploading mods for this game to mod.io (applicable if submission_option = 0).
        public string profile_url; // URL to the game's mod.io page.
        public GameTagOptionObject[] tag_options; // Groups of tags configured by the game developer, that mods can select.

        // - Equality Operators -
        public override int GetHashCode()
        {
            return this.id;
        }

        public override bool Equals(object obj)
        {
            return (obj is GameObject
                    && this.Equals((GameObject)obj));
        }

        public bool Equals(GameObject other)
        {
            return(this.id.Equals(other.id)
                   && this.status.Equals(other.status)
                   && this.submitted_by.Equals(other.submitted_by)
                   && this.date_added.Equals(other.date_added)
                   && this.date_updated.Equals(other.date_updated)
                   && this.date_live.Equals(other.date_live)
                   && this.presentation_option.Equals(other.presentation_option)
                   && this.submission_option.Equals(other.submission_option)
                   && this.curation_option.Equals(other.curation_option)
                   && this.community_options.Equals(other.community_options)
                   && this.revenue_options.Equals(other.revenue_options)
                   && this.api_access_options.Equals(other.api_access_options)
                   && this.ugc_name.Equals(other.ugc_name)
                   && this.icon.Equals(other.icon)
                   && this.logo.Equals(other.logo)
                   && this.header.Equals(other.header)
                   && this.homepage.Equals(other.homepage)
                   && this.name.Equals(other.name)
                   && this.name_id.Equals(other.name_id)
                   && this.summary.Equals(other.summary)
                   && this.instructions.Equals(other.instructions)
                   && this.profile_url.Equals(other.profile_url)
                   && this.tag_options.GetHashCode().Equals(other.tag_options.GetHashCode()));
        }
    }

    [Serializable]
    public struct GameTagOptionObject : IEquatable<GameTagOptionObject>
    {
        public string name; // Name of the tag group.
        public string type; // Can multiple tags be selected via 'checkboxes' or should only a single tag be selected via a 'dropdown'.
        public int hidden;  // Groups of tags flagged as 'admin only' should only be used for filtering, and should not be displayed to users.
        public string[] tags; // Array of tags in this group.

        // - Equality Operators -
        public override int GetHashCode()
        {
            return (this.name.GetHashCode() 
                    ^ this.type.GetHashCode());
        }

        public override bool Equals(object obj)
        {
            return (obj is GameTagOptionObject
                    && this.Equals((GameTagOptionObject)obj));
        }

        public bool Equals(GameTagOptionObject other)
        {
            return(this.name.Equals(other.name)
                   && this.type.Equals(other.type)
                   && this.hidden.Equals(other.hidden)
                   && this.tags.GetHashCode().Equals(other.tags.GetHashCode()));
        }
    }

    [Serializable]
    public struct HeaderImageObject : IEquatable<HeaderImageObject>
    {
        public string filename; // Header image filename including extension.
        public string original; // URL to the full-sized header image.

        // - Equality Operators -
        public override int GetHashCode()
        {
            return this.filename.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return (obj is HeaderImageObject
                    && this.Equals((HeaderImageObject)obj));
        }

        public bool Equals(HeaderImageObject other)
        {
            return(this.filename.Equals(other.filename)
                   && this.original.Equals(other.original));
        }
    }

    [Serializable]
    public struct IconObject : IEquatable<IconObject>
    {
        public string filename; // Icon filename including extension.
        public string original; // URL to the full-sized icon.
        public string thumb_64x64; // URL to the small thumbnail image.
        public string thumb_128x128; // URL to the medium thumbnail image.
        public string thumb_256x256; // URL to the large thumbnail image.

        // - Equality Operators -
        public override int GetHashCode()
        {
            return this.filename.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return (obj is IconObject
                    && this.Equals((IconObject)obj));
        }

        public bool Equals(IconObject other)
        {
            return(this.filename.Equals(other.filename)
                   && this.original.Equals(other.original)
                   && this.thumb_64x64.Equals(other.thumb_64x64)
                   && this.thumb_128x128.Equals(other.thumb_128x128)
                   && this.thumb_256x256.Equals(other.thumb_256x256));
        }
    }

    [Serializable]
    public struct ImageObject : IEquatable<ImageObject>
    {
        public string filename; // Image filename including extension.
        public string original; // URL to the full-sized image.
        public string thumb_320x180; // URL to the image thumbnail.

        // - Equality Operators -
        public override int GetHashCode()
        {
            return this.filename.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return (obj is ImageObject
                    && this.Equals((ImageObject)obj));
        }

        public bool Equals(ImageObject other)
        {
            return(this.filename.Equals(other.filename)
                   && this.original.Equals(other.original)
                   && this.thumb_320x180.Equals(other.thumb_320x180));
        }
    }

    [Serializable]
    public struct LogoObject : IEquatable<LogoObject>
    {
        public string filename; // Logo filename including extension.
        public string original; // URL to the full-sized logo.
        public string thumb_320x180; // URL to the small logo thumbnail.
        public string thumb_640x360; // URL to the medium logo thumbnail.
        public string thumb_1280x720; // URL to the large logo thumbnail.

        // - Equality Operators -
        public override int GetHashCode()
        {
            return this.filename.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return (obj is LogoObject
                    && this.Equals((LogoObject)obj));
        }

        public bool Equals(LogoObject other)
        {
            return(this.filename.Equals(other.filename)
                   && this.original.Equals(other.original)
                   && this.thumb_320x180.Equals(other.thumb_320x180)
                   && this.thumb_640x360.Equals(other.thumb_640x360)
                   && this.thumb_1280x720.Equals(other.thumb_1280x720));
        }
    }

    [Serializable]
    public struct ModObject : IEquatable<ModObject>
    {
        public int id;  // Unique mod id.
        public int game_id; // Unique game id.
        public int status;  // Status of the mod (see status and visibility for details):
        public int visible; // Visibility of the mod (see status and visibility for details):
        public UserObject submitted_by; // Contains user data.
        public int date_added;  // Unix timestamp of date mod was registered.
        public int date_updated;    // Unix timestamp of date mod was updated.
        public int date_live;   // Unix timestamp of date mod was set live.
        public LogoObject logo; // Contains logo data.
        public string homepage; // Official homepage of the mod.
        public string name; // Name of the mod.
        public string name_id; // Path for the mod on mod.io. For example: https://gamename.mod.io/mod-name-id-here
        public string summary; // Summary of the mod.
        public string description; // Detailed description of the mod which allows HTML.
        public string metadata_blob; // Metadata stored by the game developer. Metadata can also be stored as searchable key value pairs, and to individual mod files.
        public string profile_url; // URL to the mod's mod.io profile.
        public ModfileObject modfile; // Contains modfile data.
        public ModMediaObject media; // Contains mod media data.
        public RatingSummaryObject rating_summary; // Contains ratings summary.
        public ModTagObject[] tags; // Contains mod tag data.

        // - Equality Operators -
        public override int GetHashCode()
        {
            return this.id;
        }

        public override bool Equals(object obj)
        {
            return (obj is ModObject
                    && this.Equals((ModObject)obj));
        }

        public bool Equals(ModObject other)
        {
            return(this.id.Equals(other.id)
                   && this.game_id.Equals(other.game_id)
                   && this.status.Equals(other.status)
                   && this.visible.Equals(other.visible)
                   && this.submitted_by.Equals(other.submitted_by)
                   && this.date_added.Equals(other.date_added)
                   && this.date_updated.Equals(other.date_updated)
                   && this.date_live.Equals(other.date_live)
                   && this.logo.Equals(other.logo)
                   && this.homepage.Equals(other.homepage)
                   && this.name.Equals(other.name)
                   && this.name_id.Equals(other.name_id)
                   && this.summary.Equals(other.summary)
                   && this.description.Equals(other.description)
                   && this.metadata_blob.Equals(other.metadata_blob)
                   && this.profile_url.Equals(other.profile_url)
                   && this.modfile.Equals(other.modfile)
                   && this.media.Equals(other.media)
                   && this.rating_summary.Equals(other.rating_summary)
                   && this.tags.GetHashCode().Equals(other.tags.GetHashCode()));
        }
    }

    [Serializable]
    public struct ModDependencyObject : IEquatable<ModDependencyObject>
    {
        public int mod_id;  // Unique id of the mod that is the dependency.
        public int date_added;  // Unix timestamp of date the dependency was added.

        // - Equality Operators -
        public override int GetHashCode()
        {
            return (this.mod_id ^ this.date_added);
        }

        public override bool Equals(object obj)
        {
            return (obj is ModDependencyObject
                    && this.Equals((ModDependencyObject)obj));
        }

        public bool Equals(ModDependencyObject other)
        {
            return(this.mod_id.Equals(other.mod_id)
                   && this.date_added.Equals(other.date_added));
        }
    }

    [Serializable]
    public struct ModEventObject : IEquatable<ModEventObject>
    {
        public int id;  // Unique id of the event object.
        public int mod_id;  // Unique id of the parent mod.
        public int user_id; // Unique id of the user who performed the action.
        public int date_added;  // Unix timestamp of date the event occurred.
        public string event_type; // Type of event was 'MODFILE_CHANGED', 'MOD_AVAILABLE', 'MOD_UNAVAILABLE', 'MOD_EDITED'.

        // - Equality Operators -
        public override int GetHashCode()
        {
            return this.id;
        }

        public override bool Equals(object obj)
        {
            return (obj is ModEventObject
                    && this.Equals((ModEventObject)obj));
        }

        public bool Equals(ModEventObject other)
        {
            return(this.id.Equals(other.id)
                   && this.mod_id.Equals(other.mod_id)
                   && this.user_id.Equals(other.user_id)
                   && this.date_added.Equals(other.date_added)
                   && this.event_type.Equals(other.event_type));
        }
    }

    [Serializable]
    public struct ModfileObject : IEquatable<ModfileObject>
    {
        public int id;  // Unique modfile id.
        public int mod_id;  // Unique mod id.
        public int date_added;  // Unix timestamp of date file was added.
        public int date_scanned;    // Unix timestamp of date file was virus scanned.
        public int virus_status;    // Current virus scan status of the file. For newly added files that have yet to be scanned this field will change frequently until a scan is complete:
        public int virus_positive;  // Was a virus detected:
        public string virustotal_hash; // VirusTotal proprietary hash to view the scan results.
        public int filesize;    // Size of the file in bytes.
        public FilehashObject filehash; // Contains filehash data.
        public string filename; // Filename including extension.
        public string version; // Release version this file represents.
        public string changelog; // Changelog for the file.
        public string metadata_blob; // Metadata stored by the game developer for this file.
        public ModfileDownloadObject download; // Contains download data.

        // - Equality Operators -
        public override int GetHashCode()
        {
            return this.id;
        }

        public override bool Equals(object obj)
        {
            return (obj is ModfileObject
                    && this.Equals((ModfileObject)obj));
        }

        public bool Equals(ModfileObject other)
        {
            return(this.id.Equals(other.id)
                   && this.mod_id.Equals(other.mod_id)
                   && this.date_added.Equals(other.date_added)
                   && this.date_scanned.Equals(other.date_scanned)
                   && this.virus_status.Equals(other.virus_status)
                   && this.virus_positive.Equals(other.virus_positive)
                   && this.virustotal_hash.Equals(other.virustotal_hash)
                   && this.filesize.Equals(other.filesize)
                   && this.filehash.Equals(other.filehash)
                   && this.filename.Equals(other.filename)
                   && this.version.Equals(other.version)
                   && this.changelog.Equals(other.changelog)
                   && this.metadata_blob.Equals(other.metadata_blob)
                   && this.download.Equals(other.download));
        }
    }

    [Serializable]
    public struct ModfileDownloadObject : IEquatable<ModfileDownloadObject>
    {
        public string binary_url; // URL to download the file from the mod.io CDN.
        public int date_expires;    // Unix timestamp of when the binary_url will expire.

        // - Equality Operators -
        public override int GetHashCode()
        {
            return this.binary_url.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return (obj is ModfileDownloadObject
                    && this.Equals((ModfileDownloadObject)obj));
        }

        public bool Equals(ModfileDownloadObject other)
        {
            return(this.binary_url.Equals(other.binary_url)
                   && this.date_expires.Equals(other.date_expires));
        }
    }

    [Serializable]
    public struct ModMediaObject : IEquatable<ModMediaObject>
    {
        public string[] youtube; // Array of YouTube links.
        public string[] sketchfab; // Array of SketchFab links.
        public ImageObject[] images; // Array of image objects (a gallery).

        // - Equality Operators -
        public override int GetHashCode()
        {
            return(this.youtube.GetHashCode()
                   ^ this.sketchfab.GetHashCode()
                   ^ this.images.GetHashCode());
        }

        public override bool Equals(object obj)
        {
            return (obj is ModMediaObject
                    && this.Equals((ModMediaObject)obj));
        }

        public bool Equals(ModMediaObject other)
        {
            return(this.youtube.GetHashCode().Equals(other.youtube.GetHashCode())
                   && this.sketchfab.GetHashCode().Equals(other.sketchfab.GetHashCode())
                   && this.images.GetHashCode().Equals(other.images.GetHashCode()));
        }
    }

    [Serializable]
    public struct ModTagObject : IEquatable<ModTagObject>
    {
        public string name; // Tag name.
        public int date_added;  // Unix timestamp of date tag was applied.

        // - Equality Operators -
        public override int GetHashCode()
        {
            return (this.name.GetHashCode());
        }

        public override bool Equals(object obj)
        {
            return (obj is ModTagObject
                    && this.Equals((ModTagObject)obj));
        }

        public bool Equals(ModTagObject other)
        {
            return(this.name.Equals(other.name)
                   && this.date_added.Equals(other.date_added));
        }
    }

    [Serializable]
    public struct RatingSummaryObject : IEquatable<RatingSummaryObject>
    {
        public int total_ratings;   // Number of times this item has been rated.
        public int positive_ratings;    // Number of positive ratings.
        public int negative_ratings;    // Number of negative ratings.
        public int percentage_positive; // Number of positive ratings, divided by the total ratings to determine it’s percentage score.
        public float weighted_aggregate; // Overall rating of this item calculated using the Wilson score confidence interval. This column is good to sort on, as it will order items based on number of ratings and will place items with many positive ratings above those with a higher score but fewer ratings.
        public string display_text; // Textual representation of the rating in format:

        // - Equality Operators -
        public override int GetHashCode()
        {
            return (this.total_ratings
                    ^ this.percentage_positive);
        }

        public override bool Equals(object obj)
        {
            return (obj is RatingSummaryObject
                    && this.Equals((RatingSummaryObject)obj));
        }

        public bool Equals(RatingSummaryObject other)
        {
            return(this.total_ratings.Equals(other.total_ratings)
                   && this.positive_ratings.Equals(other.positive_ratings)
                   && this.negative_ratings.Equals(other.negative_ratings)
                   && this.percentage_positive.Equals(other.percentage_positive)
                   && this.weighted_aggregate.Equals(other.weighted_aggregate)
                   && this.display_text.Equals(other.display_text));
        }
    }

    [Serializable]
    public struct TeamMemberObject : IEquatable<TeamMemberObject>
    {
        public int id;  // Unique team member id.
        public UserObject user; // Contains user data.
        public int level;   // Level of permission the user has:
        public int date_added;  // Unix timestamp of the date the user was added to the team.
        public string position; // Custom title given to the user in this team.

        // - Equality Operators -
        public override int GetHashCode()
        {
            return this.id;
        }

        public override bool Equals(object obj)
        {
            return (obj is TeamMemberObject
                    && this.Equals((TeamMemberObject)obj));
        }

        public bool Equals(TeamMemberObject other)
        {
            return(this.id.Equals(other.id)
                   && this.user.Equals(other.user)
                   && this.level.Equals(other.level)
                   && this.date_added.Equals(other.date_added)
                   && this.position.Equals(other.position));
        }
    }

    [Serializable]
    public struct UserObject : IEquatable<UserObject>
    {
        public int id;  // Unique id of the user.
        public string name_id; // Path for the user on mod.io. For example: https://mod.io/members/username-id-here Usually a simplified version of their username.
        public string username; // Username of the user.
        public int date_online; // Unix timestamp of date the user was last online.
        public AvatarObject avatar; // Contains avatar data.
        public string timezone; // Timezone of the user, format is country/city.
        public string language; // 2-character representation of users language preference.
        public string profile_url; // URL to the user's mod.io profile.

        // - Equality Operators -
        public override int GetHashCode()
        {
            return this.id;
        }

        public override bool Equals(object obj)
        {
            return (obj is UserObject
                    && this.Equals((UserObject)obj));
        }

        public bool Equals(UserObject other)
        {
            return(this.id.Equals(other.id)
                   && this.name_id.Equals(other.name_id)
                   && this.username.Equals(other.username)
                   && this.date_online.Equals(other.date_online)
                   && this.avatar.Equals(other.avatar)
                   && this.timezone.Equals(other.timezone)
                   && this.language.Equals(other.language)
                   && this.profile_url.Equals(other.profile_url));
        }
    }
}
