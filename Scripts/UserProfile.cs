using SerializeField = UnityEngine.SerializeField;

namespace ModIO
{
    [System.Serializable]
    public class UserProfile
    {
        // ---------[ SERIALIZED MEMBERS ]---------
        [SerializeField] private int _id;
        [SerializeField] private string _nameId;
        [SerializeField] private string _username;
        [SerializeField] private TimeStamp _dateOnline;
        // TODO(@jackson): Replace with identifier
        [SerializeField] private AvatarImageSet _avatar;
        [SerializeField] private string _timezone;
        [SerializeField] private string _language;
        [SerializeField] private string _profileURL;

        // ---------[ FIELDS ]---------
        public int id                   { get { return this._id; } }
        public string nameId            { get { return this._nameId; } }
        public string username          { get { return this._username; } }
        public TimeStamp dateOnline     { get { return this._dateOnline; } }
        public AvatarImageSet avatar   { get { return this._avatar; } }
        public string timezone          { get { return this._timezone; } }
        public string language          { get { return this._language; } }
        public string profileURL        { get { return this._profileURL; } }

        // ---------[ API OBJECT INTERFACE ]---------
        public void ApplyUserObjectValues(API.UserObject apiObject)
        {
            this._id = apiObject.id;
            this._nameId = apiObject.name_id;
            this._username = apiObject.username;
            this._dateOnline = TimeStamp.GenerateFromServerTimeStamp(apiObject.date_online);
            this._avatar = AvatarImageSet.CreateFromAvatarObject(apiObject.avatar);
            this._timezone = apiObject.timezone;
            this._language = apiObject.language;
            this._profileURL = apiObject.profile_url;
        }

        public static UserProfile CreateFromUserObject(API.UserObject apiObject)
        {
            var retVal = new UserProfile();
            retVal.ApplyUserObjectValues(apiObject);
            return retVal;
        }
    }
}
