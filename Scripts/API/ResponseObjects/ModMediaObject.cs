namespace ModIO.API
{
    [System.Serializable]
    public struct ModMediaObject
    {
        // Array of YouTube links.
        public string[] youtube;
        // Array of SketchFab links.
        public string[] sketchfab;
        // Array of image objects (a gallery).
        public ImageObject[] images;
    }
}
