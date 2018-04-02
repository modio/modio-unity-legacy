namespace ModIO.API
{
    [System.Serializable]
    public struct ModMediaObject
    {
        // Array of YouTube links.
        public readonly string[] youtube;
        // Array of SketchFab links.
        public readonly string[] sketchfab;
        // Array of image objects (a gallery).
        public readonly ImageObject[] images;
    }
}
