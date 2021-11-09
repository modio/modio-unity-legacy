namespace ModIO.API
{
    public static class GetAllUsersFilterFields
    {
        // (integer) Unique id of the user.
        public const string id = "id";
        // (string)  Path for the user on mod.io. For example:
        // https://mod.io/members/username-id-here Usually a simplified version of their username.
        public const string nameId = "name_id";
        // (integer) Unix timestamp of date the user was last online.
        public const string dateOnline = "date_online";
        // (string)  Username of the user.
        public const string username = "username";
        // (string)  Timezone of the user, format is country/city.
        public const string timezone = "timezone";
        // (string)  2-character representation of language.
        public const string languageCode = "language";
    }
}
