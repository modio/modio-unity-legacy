using System.Collections.Generic;

namespace ModIO
{
    [System.Serializable]
    public struct UserData
    {
        public static readonly UserData NONE = new UserData()
        {
            userId = UserProfile.NULL_ID,
            token = string.Empty,
            profile = null,
            subscribedMods = new List<int>(),
            enabledMods = new List<int>(),
        };

        public int userId;
        public string token;

        public UserProfile profile;
        public List<int> subscribedMods;
        public List<int> enabledMods;
    }
}
