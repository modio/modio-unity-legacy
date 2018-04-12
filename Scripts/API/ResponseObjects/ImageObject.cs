namespace ModIO.API
{
    [System.Serializable]
    public struct ImageObject
    {
        // Image filename including extension.
        public string filename;
        // URL to the full-sized image.
        public string original;
        // URL to the image thumbnail.
        public string thumb_320x180;
    }
}
