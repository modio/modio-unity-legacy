namespace ModIO.UI
{
    [System.Obsolete("No longer supported.")]
    [System.Serializable]
    public struct UserProfileDisplayData
    {
        public int userId;
        public string nameId;
        public string username;
        public int lastOnline;
        public string timezone;
        public string language;
        public string profileURL;

        public static UserProfileDisplayData CreateFromProfile(UserProfile profile)
        {
            UserProfileDisplayData userData = new UserProfileDisplayData() {
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

    [System.Obsolete("No longer supported.")]
    public abstract class UserProfileDisplayComponent : UnityEngine.MonoBehaviour
    {
        public abstract event System.Action<UserProfileDisplayComponent> onClick;

        public abstract UserProfileDisplayData data { get; set; }

        public abstract void Initialize();
        public abstract void DisplayProfile(UserProfile profile);
        public abstract void DisplayLoading();
    }
}
