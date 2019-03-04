using Newtonsoft.Json;

namespace ModIO
{
    [System.Serializable]
    public class ModTeamMember
    {
        // ---------[ FIELDS ]---------
        /// <summary>
        /// Unique team member id.
        /// </summary>
        [JsonProperty("id")]
        public int id;

        /// <summary>
        /// Contains user data.
        /// </summary>
        [JsonProperty("user")]
        public UserProfile user;

        /// <summary>
        /// Level of permission the user has:
        /// </summary>
        [JsonProperty("level")]
        public ModTeamMemberAccessLevel accessLevel;

        /// <summary>
        /// Unix timestamp of the date the user was added to the team.
        /// </summary>
        [JsonProperty("date_added")]
        public int dateAdded;

        /// <summary>
        /// Custom title given to the user in this team.
        /// </summary>
        [JsonProperty("position")]
        public string title;
    }
}
