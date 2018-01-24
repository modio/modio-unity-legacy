using System;

namespace ModIO
{
    [Serializable]
    public class User : IEquatable<User>
    {
        // - Constructors - 
        public static User GenerateFromAPIObject(API.UserObject apiObject)
        {
            User newUser = new User();
            newUser._data = apiObject;

            newUser.dateOnline = TimeStamp.GenerateFromServerTimeStamp(apiObject.date_online);
            newUser.avatar = AvatarInfo.GenerateFromAPIObject(apiObject.avatar);

            return newUser;
        }

        public static User[] GenerateFromAPIObjectArray(API.UserObject[] apiObjectArray)
        {
            User[] objectArray = new User[apiObjectArray.Length];

            for(int i = 0;
                i < apiObjectArray.Length;
                ++i)
            {
                objectArray[i] = User.GenerateFromAPIObject(apiObjectArray[i]);
            }

            return objectArray;
        }

        // - Fields -
        [UnityEngine.SerializeField]
        private API.UserObject _data;

        public int id               { get { return _data.id; } }
        public string nameId        { get { return _data.name_id; } }
        public string username      { get { return _data.username; } }
        public TimeStamp dateOnline { get; private set; }
        public AvatarInfo avatar    { get; private set; }
        public string timezone      { get { return _data.timezone; } }
        public string language      { get { return _data.language; } }
        public string profileURL    { get { return _data.profile_url; } }

        // - Equality Overrides -
        public override int GetHashCode()
        {
            return this._data.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as User);
        }

        public bool Equals(User other)
        {
            return (Object.ReferenceEquals(this, other)
                    || this._data.Equals(other._data));
        }
    }
}
