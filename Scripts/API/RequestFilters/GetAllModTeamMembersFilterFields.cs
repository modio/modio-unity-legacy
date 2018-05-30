namespace ModIO.API
{
    public static class GetAllModTeamMembersFilterFields
    {
        // (integer) Unique id of the team member record.
        public const string id = "id";
        // (integer) Unique id of the user.
        public const string userId = "user_id";
        // (string)  Username of the user.
        public const string username = "username";
        // (integer) Level of permission the user has:
        public const string accessLevel = "level";
        // (integer) Unix timestamp of the date the user was added to the team.
        public const string dateAdded = "date_added";
        // (string)  Custom title given to the user in this team.
        public const string title = "position";
    }
}
