namespace ModIO.UI
{
    [System.Serializable]
    public struct UserDisplayData
    {
        public int      userId;
        public string   nameId;
        public string   username;
        public int      lastOnline;
        public string   timezone;
        public string   language;
        public string   profileURL;

        public static UserDisplayData CreateFromProfile(UserProfile profile)
        {
            UserDisplayData userData = new UserDisplayData()
            {
                userId = profile.id,
                nameId = profile.nameId,
                username = profile.username,
                lastOnline = profile.lastOnline,
                timezone = profile.timezone,
                language = profile.language,
                profileURL = profile.profileURL,
            };
            return userData;
        }
    }

    public abstract class UserDisplayComponent : UnityEngine.MonoBehaviour
    {
        public abstract event System.Action<UserDisplayComponent> onClick;

        public abstract UserDisplayData data { get; set; }

        public abstract void Initialize();
        public abstract void DisplayProfile(UserProfile profile);
        public abstract void DisplayLoading();
    }
}
