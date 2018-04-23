namespace ModIO.API
{
    [System.Serializable]
    public struct TeamMemberObject
    {
        // - Value Interpretation -
        public static class LevelValue
        {
            public const int Moderator = 1;  // (can moderate comments and content attached)
            public const int Creator = 4;  // (moderator access, including uploading builds and edit all settings except supply and team members)
            public const int Administrator = 8;  // (full access, including editing the supply and team)
        }

        // Unique team member id.
        public int id;
        // Level of permission the user has: see LevelValue
        public int level;
        // Unix timestamp of the date the user was added to the team.
        public int date_added;
        // Custom title given to the user in this team.
        public string position;
        // Contains user data.
        public UserObject user;
    }
}
