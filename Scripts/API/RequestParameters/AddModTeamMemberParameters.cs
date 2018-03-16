namespace ModIO.API
{
    public class AddModTeamMemberParameters : RequestParameters
    {
        // ---------[ FIELDS ]---------
        // [REQUIRED] Email of the mod.io user you want to add to your team.
        public string email
        {
            set
            {
                this.SetStringValue("email", value);
            }
        }
        // [REQUIRED] Level of permission the user will get:
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