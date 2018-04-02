using SerializeField = UnityEngine.SerializeField;

namespace ModIO
{
    // - Enums -
    public enum TeamMemberPermissionLevel
    {
        Guest = 0,
        Member = 1,
        Contributor = 2,
        Manager = 4,
        Leader = 8,
    }

    [System.Serializable]
    public class TeamMemberInfo
    {
        // ---------[ SERIALIZED MEMBERS ]---------
        [SerializeField] private int _id;
        [SerializeField] private int _userId;
        [SerializeField] private TeamMemberPermissionLevel _permissionLevel;
        [SerializeField] private TimeStamp _dateAdded;
        [SerializeField] private string _title;

        // ---------[ FIELDS ]---------
        public int id                                       { get { return this._id; } }
        public int userId                                   { get { return this._userId; } }
        public TeamMemberPermissionLevel permissionLevel    { get { return this._permissionLevel; } }
        public TimeStamp dateAdded                          { get { return this._dateAdded; } }
        public string title                                 { get { return this._title; } }

        // ---------[ API OBJECT INTERFACE ]---------
        public void ApplyTeamMemberObjectValues(API.TeamMemberObject apiObject)
        {
            this._id = apiObject.id;
            this._userId = apiObject.user.id;
            this._permissionLevel = (TeamMemberPermissionLevel)apiObject.level;
            this._dateAdded = TimeStamp.GenerateFromServerTimeStamp(apiObject.date_added);
            this._title = apiObject.position;
        }

        public static TeamMemberInfo CreateFromTeamMemberObject(API.TeamMemberObject apiObject)
        {
            var retVal = new TeamMemberInfo();
            retVal.ApplyTeamMemberObjectValues(apiObject);
            return retVal;
        }
    }
}
