namespace ModIO
{
    [System.Serializable]
    public struct UserAuthenticationData
    {
        public static readonly UserAuthenticationData NONE = new UserAuthenticationData() { userId = 0, token = null };

        public int userId;
        public string token;
    }
}
