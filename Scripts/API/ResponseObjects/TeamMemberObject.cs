namespace ModIO.API
{
    [System.Serializable]
    public struct TeamMemberObject
    {
        // Unique team member id.
        public int id;
        // Level of permission the user has:
        public int level;
        // Unix timestamp of the date the user was added to the team.
        public int date_added;
        // Custom title given to the user in this team.
        public string position;
        // Contains user data.
        public UserObject user;
    }
}