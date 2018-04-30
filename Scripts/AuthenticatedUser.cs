using System.Collections.Generic;

namespace ModIO
{
    [System.Serializable]
    public class AuthenticatedUser
    {
        public string oAuthToken;
        public UserProfile profile;
        public List<int> subscribedModIDs;
    }
}