namespace ModIO.API
{
    [System.Serializable]
    public struct AvatarObject
    {
        // Avatar filename including extension.
        public string filename;
        // URL to the full-sized avatar.
        public string original;
        // URL to the small thumbnail image.
        public string thumb_50x50;
        // URL to the medium thumbnail image.
        public string thumb_100x100;
    }
}