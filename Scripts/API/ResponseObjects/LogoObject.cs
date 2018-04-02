namespace ModIO.API
{
    [System.Serializable]
    public struct LogoObject
    {
        // Logo filename including extension.
        public readonly string filename;
        // URL to the full-sized logo.
        public readonly string original;
        // URL to the small logo thumbnail.
        public readonly string thumb_320x180;
        // URL to the medium logo thumbnail.
        public readonly string thumb_640x360;
        // URL to the large logo thumbnail.
        public readonly string thumb_1280x720;
    }
}