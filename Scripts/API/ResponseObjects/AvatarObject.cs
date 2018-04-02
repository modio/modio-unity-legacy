namespace ModIO.API
{
    [System.Serializable]
    public struct AvatarObject
    {
        // Avatar filename including extension.
        public readonly string filename;
        // URL to the full-sized avatar.
        public readonly string original;
        // URL to the small thumbnail image.
        public readonly string thumb_50x50;
        // URL to the medium thumbnail image.
        public readonly string thumb_100x100;
    }
}