namespace ModIO.API
{
    [System.Serializable]
    public struct IconObject
    {
        // Icon filename including extension.
        public string filename;
        // URL to the full-sized icon.
        public string original;
        // URL to the small thumbnail image.
        public string thumb_64x64;
        // URL to the medium thumbnail image.
        public string thumb_128x128;
        // URL to the large thumbnail image.
        public string thumb_256x256;
    }
}