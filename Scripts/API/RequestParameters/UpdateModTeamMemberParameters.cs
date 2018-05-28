namespace ModIO.API
{
    public class UpdateModTeamMemberParameters : RequestParameters
    {
        // ---------[ FIELDS ]---------
        // Level of permission the user should have:
        public int level
        {
            set
            {
                this.SetStringValue("level", value);
            }
        }
        // Title of the users position. For example: 'Team Leader', 'Artist'.
        public string position
        {
            set
            {
                this.SetStringValue("position", value);
            }
        }
    }
}