using System.Collections.Generic;

// TODO(@jackson): Add accessors to pretty up the names
// TODO(@jackson): Recheck the objects against the documentation
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

        public User submitted_by;
        public ImageData icon;
        public Logo logo;
        // public Header header;
        public TagCategory[] cats;
    }

    [System.Serializable]
    public class Mod
    {
        public int id;  // int32)  Unique mod id.
        public int game;    // int32)  Unique game id.
        public User submitted_by;    // Contains member data.
        public float price;     // Sale price if applicable, in USD.
        public int datereg;     // int32)  Unix timestamp of date registered.
        public int dateup;  // int32)  Unix timestamp of date last updated.
        public Logo logo;    // Contains logo data.
        public string homepage;     // Mod homepage URL.
        public string name;     // Name of the mod.
        public string nameid;   // Unique SEO-friendly mod uri.
        public string summary;  // Brief summary of the mod.
        public string description;  // Description of the mod.
        public string metadata;     // Metadata for the mod.
        public string url;  // Official website url for the mod.
        public ModFile modfile;  // Contains file data.
        public object media;    // Contains media data.
        public Rating ratings;   // Contains ratings data.
        public ModTag[] tags;     // Contains Tags data.

        // Accessors
        public Rating rating
        {
            get { return ratings; }
            set { ratings = value; }
        }

        public IEnumerable<string> tagStrings
        {
            get
            {
                foreach(ModTag tag in tags)
                {
                    yield return tag.tag;
                }
            }
        }
    }

    [System.Serializable]
    public class Rating
    {
        public int total;  // (int32)  Total ratings count.
        public int positive;  // int32)  Positive ratings count.
        public int negative;  // int32)  Negative ratings count.
        public float weighted;  // Weighted rating taking into account positive & negative ratings.
        public int percentage;  // (int32)  Rating of the mod as a percentage.
        public int stars;  // (int32)  The amount of stars the mod has, between 0 and 5.
        public string text;  // Text representation of the rating total.
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
    public class ModTag
    {
        // public int game; // Eg: 8,
        // public int mod; // Eg: 41,
        public int date; // Eg: 1508132357,
        public string tag; // Eg: "Weapon"
    }

    [System.Serializable]
    public class User
    {
        public int id; // (int32)  Unique id of the user.
        public string nameid; // Unique nameid of user which forms end of their profile URL.
        public string username; // Non-unique username of the user.
        public int online; // (int32)  Unix timestamp on when the user was last online.
        public string timezone; // The Timezone of the user, shown in {Country}/{City} format.
        public string language; // The users language preference, limited to two characters.
        public string url; // URL to the user profile.

        // public Avatar avatar; // Contains avatar data.
    }

    [System.Serializable]
    public class ModFile
    {
        public int id; // Unique id of the file.
        public int mod; // Unique id of the mod.
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
        public User member; // Contains member data.
        public int dateup; // (int32)  Unix timestamp of when the update occurred.
        public string _event; // The type of resource and action that occurred.
        public object changes; // No description
    }
}
