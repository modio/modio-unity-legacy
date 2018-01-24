using System;

namespace ModIO
{
    [Serializable]
    public class User : IEquatable<User>, IAPIObjectWrapper<API.UserObject>, UnityEngine.ISerializationCallbackReceiver
    {
        // - Fields -
        [UnityEngine.SerializeField]
        private API.UserObject _data;

        public int id               { get { return _data.id; } }
        public string nameId        { get { return _data.name_id; } }
        public string username      { get { return _data.username; } }
        public TimeStamp dateOnline { get; private set; }
        public AvatarURLInfo avatar { get; private set; }
        public string timezone      { get { return _data.timezone; } }
        public string language      { get { return _data.language; } }
        public string profileURL    { get { return _data.profile_url; } }
        
        // - IAPIObjectWrapper Interface -
        public void WrapAPIObject(API.UserObject apiObject)
        {
            this._data = apiObject;

            this.dateOnline = TimeStamp.GenerateFromServerTimeStamp(apiObject.date_online);
            this.avatar = new AvatarURLInfo();
            this.avatar.WrapAPIObject(apiObject.avatar);
        }

        public API.UserObject GetAPIObject()
        {
            return this._data;
        }

        // - ISerializationCallbackReceiver -
        public void OnBeforeSerialize() {}
        public void OnAfterDeserialize()
        {
            this.WrapAPIObject(this._data);
        }

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
