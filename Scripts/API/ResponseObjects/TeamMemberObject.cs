namespace ModIO.API
{
    [System.Serializable]
    public struct TeamMemberObject
    {
        // Unique team member id.
        public readonly int id;
        // Level of permission the user has:
        public readonly int level;
        // Unix timestamp of the date the user was added to the team.
        public readonly int date_added;
        // Custom title given to the user in this team.
        public readonly string position;
        // Contains user data.
        public readonly UserObject user;
    }
}