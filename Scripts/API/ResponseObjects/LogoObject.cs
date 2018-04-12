namespace ModIO.API
{
    [System.Serializable]
    public struct LogoObject
    {
        // Logo filename including extension.
        public string filename;
        // URL to the full-sized logo.
        public string original;
        // URL to the small logo thumbnail.
        public string thumb_320x180;
        // URL to the medium logo thumbnail.
        public string thumb_640x360;
        // URL to the large logo thumbnail.
        public string thumb_1280x720;
    }
}