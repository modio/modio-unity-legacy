using System;

namespace ModIO.API
{
    [Serializable]
    public struct TeamMemberObject : IEquatable<TeamMemberObject>
    {
        // - Fields -
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
    public struct CreatedTeamMember
    {
        // --- FIELDS ---
        // [Required] Email of the mod.io user you want to add to your team.
        public string email;
        // [Required] Level of permission the user will get
        public int level;
        // Title of the users position. For example: 'Team Leader', 'Artist'.
        public string position;
    }
}