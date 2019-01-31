namespace ModIO
{
    [System.Serializable]
    public struct UserAuthenticationData
    {
        public static readonly UserAuthenticationData NONE = new UserAuthenticationData()
        {
            userId = UserProfile.NULL_ID,
            token = null,
        };

        public int userId;
        public string token;
    }
}
