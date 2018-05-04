using SerializeField = UnityEngine.SerializeField;

namespace ModIO
{
    public enum UserAvatarVersion
    {
        FullSize = 0,
        Thumbnail_50x50,
        Thumbnail_100x100,
    }

    [System.Serializable]
    public class UserProfile
    {
        // ---------[ SERIALIZED MEMBERS ]---------
        [SerializeField] private int _id;
        [SerializeField] private string _nameId;
        [SerializeField] private string _username;
        [SerializeField] private int _dateOnline;
        // TODO(@jackson): Replace with identifier
        [SerializeField] private AvatarImageLocator _avatarLocator;
        [SerializeField] private string _timezone;
        [SerializeField] private string _language;
        [SerializeField] private string _profileURL;

        // ---------[ FIELDS ]---------
        public int id                           { get { return this._id; } }
        public string nameId                    { get { return this._nameId; } }
        public string username                  { get { return this._username; } }
        public int dateOnline             { get { return this._dateOnline; } }
        public AvatarImageLocator avatarLocator { get { return this._avatarLocator; } }
        public string timezone                  { get { return this._timezone; } }
        public string language                  { get { return this._language; } }
        public string profileURL                { get { return this._profileURL; } }

        // ---------[ API OBJECT INTERFACE ]---------
        public void ApplyUserObjectValues(API.UserObject apiObject)
        {
            this._id = apiObject.id;
            this._nameId = apiObject.nameId;
            this._username = apiObject.username;
            this._dateOnline = apiObject.dateOnline;
            this._avatarLocator = apiObject.avatarLocator;
            this._timezone = apiObject.timezone;
            this._language = apiObject.language;
            this._profileURL = apiObject.profileURL;
        }

        public static UserProfile CreateFromUserObject(API.UserObject apiObject)
        {
            var retVal = new UserProfile();
            retVal.ApplyUserObjectValues(apiObject);
            return retVal;
        }
    }
}
