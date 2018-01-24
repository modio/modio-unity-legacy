using System;

namespace ModIO
{
    [Serializable]
    public class TeamMember : IEquatable<TeamMember>, IAPIObjectWrapper<API.TeamMemberObject>, UnityEngine.ISerializationCallbackReceiver
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

        // - IAPIObjectWrapper Interface -
        public void WrapAPIObject(API.TeamMemberObject apiObject)
        {
            this._data = apiObject;

            this.user = new User();
            this.user.WrapAPIObject(apiObject.user);
            this.dateAdded = TimeStamp.GenerateFromServerTimeStamp(apiObject.date_added);
        }

        public API.TeamMemberObject GetAPIObject()
        {
            return this._data;
        }

        // - Fields -
        [UnityEngine.SerializeField]
        private API.TeamMemberObject _data;

        public int id                           { get { return _data.id; } }
        public User user                        { get; private set; }
        public PermissionLevel permissionLevel  { get { return (PermissionLevel)_data.level; } }
        public TimeStamp dateAdded              { get; private set; }
        public string position                  { get { return _data.position; } }

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
            return this.Equals(obj as TeamMember);
        }

        public bool Equals(TeamMember other)
        {
            return (Object.ReferenceEquals(this, other)
                    || this._data.Equals(other._data));
        }
    }
}
