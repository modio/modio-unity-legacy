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

        public UnityEngine.Texture2D avatarTexture;
    }
}
