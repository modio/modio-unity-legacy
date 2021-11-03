namespace ModIO
{
    public enum ModTeamMemberAccessLevel
    {
        Moderator = 1, // can moderate comments and content attached
        Manager = 4, // moderator access, including uploading builds and editing settings except
                     // supply and team members
        Administrator = 8, // full access, including editing the supply and team

        [System.Obsolete("Replaced by ModTeamMemberAccessLevel.Manager")] Statistics = 4,
    }
}
