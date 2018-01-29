using System;
using System.Collections.Generic;

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

    public class UnsubmittedTeamMember
    {
        // --- FIELDS ---
        [UnityEngine.SerializeField]
        private API.CreatedTeamMember _data;

        // [Required] Email of the mod.io user you want to add to your team.
        public string email
        {
            get { return _data.email; }
            set { _data.email = value; }
        }

        // [Required] Level of permission the user will get
        public TeamMember.PermissionLevel permissionLevel
        {
            get { return (TeamMember.PermissionLevel)_data.level; }
            set { _data.level = (int)value; }
        }
        // Title of the users position. For example: 'Team Leader', 'Artist'.
        public string position
        {
            get { return _data.position; }
            set { _data.position = value; }
        }

        // --- ACCESSORS ---
        public StringValueField[] GetValueFields()
        {
            List<StringValueField> retVal = new List<StringValueField>();

            retVal.Add(StringValueField.Create("email", _data.email));
            retVal.Add(StringValueField.Create("level", _data.level));
            retVal.Add(StringValueField.Create("position", _data.position));

            return retVal.ToArray();
        }
    }
}
