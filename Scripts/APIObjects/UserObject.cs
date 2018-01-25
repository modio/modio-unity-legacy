using System;

namespace ModIO.API
{
    [Serializable]
    public struct UserObject : IEquatable<UserObject>
    {
        // - Fields -
        public int id;  // Unique id of the user.
        public string name_id; // Path for the user on mod.io. For example: https://mod.io/members/username-id-here Usually a simplified version of their username.
        public string username; // Username of the user.
        public int date_online; // Unix timestamp of date the user was last online.
        public AvatarObject avatar; // Contains avatar data.
        public string timezone; // Timezone of the user, format is country/city.
        public string language; // 2-character representation of users language preference.
        public string profile_url; // URL to the user's mod.io profile.

        // - Equality Operators -
        public override int GetHashCode()
        {
            return this.id;
        }

        public override bool Equals(object obj)
        {
            return (obj is UserObject
                    && this.Equals((UserObject)obj));
        }

        public bool Equals(UserObject other)
        {
            return(this.id.Equals(other.id)
                   && this.name_id.Equals(other.name_id)
                   && this.username.Equals(other.username)
                   && this.date_online.Equals(other.date_online)
                   && this.avatar.Equals(other.avatar)
                   && this.timezone.Equals(other.timezone)
                   && this.language.Equals(other.language)
                   && this.profile_url.Equals(other.profile_url));
        }
    }
}