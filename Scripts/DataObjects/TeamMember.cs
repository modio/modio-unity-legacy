using System;

namespace ModIO
{
    [Serializable]
    public class TeamMember : IEquatable<TeamMember>
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

        // - Constructors - 
        public static TeamMember GenerateFromAPIObject(API.TeamMemberObject apiObject)
        {
            TeamMember newTeamMember = new TeamMember();
            newTeamMember._data = apiObject;

            newTeamMember.user      = User.GenerateFromAPIObject(apiObject.user);
            newTeamMember.dateAdded = TimeStamp.GenerateFromServerTimeStamp(apiObject.date_added);

            return newTeamMember;
        }

        public static TeamMember[] GenerateFromAPIObjectArray(API.TeamMemberObject[] apiObjectArray)
        {
            TeamMember[] objectArray = new TeamMember[apiObjectArray.Length];

            for(int i = 0;
                i < apiObjectArray.Length;
                ++i)
            {
                objectArray[i] = TeamMember.GenerateFromAPIObject(apiObjectArray[i]);
            }

            return objectArray;
        }

        // - Fields -
        [UnityEngine.SerializeField]
        private API.TeamMemberObject _data;

        public int id                           { get { return _data.id; } }
        public User user                        { get; private set; }
        public PermissionLevel permissionLevel  { get { return (PermissionLevel)_data.level; } }
        public TimeStamp dateAdded              { get; private set; }
        public string position                  { get { return _data.position; } }

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
