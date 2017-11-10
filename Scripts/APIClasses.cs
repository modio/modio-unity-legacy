using System.Collections.Generic;

// TODO(@jackson): Add accessors to pretty up the names
namespace ModIO
{
    public enum LogoVersion
    {
        Full = 0,
        Thumb_320x180,
        Thumb_640x360,
        Thumb_1280x720
    }

    [System.Serializable]
    public class Game
    {
        public int id;
        public int datereg; // Eg. 1493702614,
        public int dateup; // Eg. 1499410290,
        public int presentation; // Eg. 1,
        public int community; // Eg. 3,
        public int submission; // Eg. 0,
        public int curation; // Eg. 0,
        public int revenue; // Eg. 1500,
        public int api; // Eg. 3,
        public string ugcname; // Eg. "map",
        public string homepage; // Eg. "https://www.rogue-knight-game.com/",
        public string name; // Eg. "Rogue Knight",
        public string nameid; // Eg. "rogue-knight",
        public string summary; // Eg. "Rogue Knight is a brand new 2D pixel platformer.",
        public string instructions; // Eg. "Instructions here on how to develop for your game.",
        public string url; // Eg. "https://rogue-knight.mod.io"

        public Member member;
        public ImageData icon;
        public Logo logo;
        // public Header header;
        public TagCategory[] cats;
    }

    [System.Serializable]
    public class Mod
    {
        public int id; // Unique id of the mod.
        public int game; // Unique id of the parent game.
        public Member member; // Unique id of the member who has ownership of the game.
        public double price; //  Numeric representation of the price.
        public int datereg; // Unix timestamp of date registered.
        public int dateup; // Unix timestamp of date updated.
        public Logo logo;
        public string homepage; //  Official homepage of the mod.
        public string name; //  Name of the mod.
        public string nameid; //  The unique SEO friendly URL for your game.
        public string summary; //  Summary of the mod.
        public string description; //  An extension of the summary. HTML Supported.
        public string metadata; //  Comma-separated list of metadata words.
        public string url;
        // media
        public int modfile; // Unique id of the file object marked as current release.
        public Tag[] tags;

        // public string status; //  OAuth 2 only. The status of the mod (only recognised by game admins), default is 'auth'.

        // Accessors
        public IEnumerable<string> tagStrings
        {
            get
            {
                foreach(Tag tag in tags)
                {
                    yield return tag.tag;
                }
            }
        }
    }

    [System.Serializable]
    public class Member
    {
        public int id;
        public string nameid;
        public string username;
        public int online; // DateTime?
        // avatar
        // "timezone":"Australia\/Brisbane",
        // "language":"en",
        // "url":"https:\/\/mod.io\/members\/melodatron"
    }

    [System.Serializable]
    public class TagCategory
    {
        public string name;
        public string type;
        public string[] tags;
        public int adminonly;
    }

    [System.Serializable]
    public class Tag
    {
        public int game; // Eg: 8,
        public int mod; // Eg: 41,
        public int date; // Eg: 1508132357,
        public string tag; // Eg: "Weapon"
    }

    [System.Serializable]
    public class User
    {
        public int id; // Unique id of the user.
        public string nameid;  // SEO-friendly representation of the username. This is the same field that forms the URL link to their profile.
        public string username;  // Username of the member.
        public string permission;  // Status of the user account.
        // permission: Field Options 0 = Unauthorized 1 = Authorized 2 = Banned 3 = Archived 4 = Deleted
        public string timezone;  // Timezone of the user, format is country/city.
        public string language;  // 2-character representation of language.
    }

    [System.Serializable]
    public class ModFile
    {
        public int id; // Unique id of the file.
        public int mod; // Unique id of the mod.
        public Member member; // Unique id of the member who published the file.
        public int date; // Unix timestamp of date added.
        public int datevirus; // Date it was last virus checked.
        public int virusstatus; // Current filescan status of the file. For newly added files that have yet to be scanned this field could change frequently until a scan is complete.
        // virusstats: Field Options 0 = Not scanned 1 = Scan complete 2 = In progress 3 = Too large to scan 4 = File not found 5 = Error Scanning
        public int viruspositive; // Virus status of file
        // viruspositive: Field Options 0 = No threats detected 1 = Flagged as malicious
        public int filesize; // Filesize of file in bytes.
        public string filehash; //  MD5 hash of file.
        public string filename; //  Filename including extension.
        public string version; //  Version of file.
        public string virustotal; //  Virustotal report.
        public string changelog; //  The changelog for the file.
        public string download; //  File download URL.
    }

    [System.Serializable]
    public class Logo
    {
        public string full;
        public string thumb_320x180;
        public string thumb_640x360;
        public string thumb_1280x720;
        public string filename;
    }

    [System.Serializable]
    public class ImageData
    {
        public string full;
        public string thumbnail;
        public string filename;
    }

    [System.Serializable]
    public class ModActivity
    {
        public int id; // (int32)  Unique id of activity object.
        public Member member; // Contains member data.
        public int dateup; // (int32)  Unix timestamp of when the update occurred.
        public string _event; // The type of resource and action that occurred.
        public object changes; // No description
    }
}
