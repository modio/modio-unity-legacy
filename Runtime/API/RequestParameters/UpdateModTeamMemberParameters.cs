namespace ModIO.API
{
    public class UpdateModTeamMemberParameters : RequestParameters
    {
        // ---------[ FIELDS ]---------
        // Level of permission the user should have:
        public ModTeamMemberAccessLevel accessLevel
        {
            set {
                this.SetStringValue("level", (int)value);
            }
        }

        // Title of the users position. For example: 'Team Leader', 'Artist'.
        public string title
        {
            set {
                this.SetStringValue("position", value);
            }
        }
    }
}
