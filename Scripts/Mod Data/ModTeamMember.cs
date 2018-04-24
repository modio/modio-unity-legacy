using SerializeField = UnityEngine.SerializeField;
using TeamMemberObject = ModIO.API.TeamMemberObject;

namespace ModIO
{
    public enum TeamMemberPermissionLevel
    {
        Moderator =     TeamMemberObject.LevelValue.Moderator,
        Creator =       TeamMemberObject.LevelValue.Creator,
        Administrator = TeamMemberObject.LevelValue.Administrator,
    }

    [System.Serializable]
    public class TeamMember
    {
        // ---------[ SERIALIZED MEMBERS ]---------
        [SerializeField] private int _userId;
        [SerializeField] private TeamMemberPermissionLevel _permissionLevel;
        [SerializeField] private TimeStamp _dateAdded;
        [SerializeField] private string _title;

        // ---------[ FIELDS ]---------
        public int userId                                   { get { return this._userId; } }
        public TeamMemberPermissionLevel permissionLevel    { get { return this._permissionLevel; } }
        public TimeStamp dateAdded                          { get { return this._dateAdded; } }
        public string title                                 { get { return this._title; } }

        // ---------[ API OBJECT INTERFACE ]---------
        public void ApplyTeamMemberObjectValues(TeamMemberObject apiObject)
        {
            this._userId = apiObject.user.id;
            this._permissionLevel = (TeamMemberPermissionLevel)apiObject.level;
            this._dateAdded = TimeStamp.GenerateFromServerTimeStamp(apiObject.date_added);
            this._title = apiObject.position;
        }
    }
}