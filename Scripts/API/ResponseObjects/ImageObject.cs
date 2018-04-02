namespace ModIO.API
{
    [System.Serializable]
    public struct ImageObject
    {
        // Image filename including extension.
        public readonly string filename;
        // URL to the full-sized image.
        public readonly string original;
        // URL to the image thumbnail.
        public readonly string thumb_320x180;
    }
}
