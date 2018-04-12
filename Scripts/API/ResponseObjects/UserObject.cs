namespace ModIO.API
{
    [System.Serializable]
    public struct UserObject
    {
        // Unique id of the user.
        public int id;
        // Path for the user on mod.io. For example: https//mod.io/members/username-id-here Usually a simplified version of their username.
        public string name_id;
        // Username of the user.
        public string username;
        // Unix timestamp of date the user was last online.
        public int date_online;
        // Timezone of the user, format is country/city.
        public string timezone;
        // Users language preference. See localization for the supported languages.
        public string language;
        // URL to the user's mod.io profile.
        public string profile_url;
        // Contains avatar data.
        public AvatarObject avatar;
    }
}
