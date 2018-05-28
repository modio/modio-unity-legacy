namespace ModIO
{
    public enum ModTeamMemberAccessLevel
    {
        Moderator = 1, // can moderate content submitted
        Statistics = 4, // moderator access, including read only access to view reports
        Administrator = 8, // full access, including editing the profile and team
    }
}
